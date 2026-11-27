using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace InnovatorSchema;

internal static class Limits
{
    public const int Characters = 1024 * 1024;
    public const int XmlDepth = 64;
    public const int XmlNodes = 100_000;
    public const int Items = 10_000;
    public const int Steps = 250_000;
    public const int Issues = 100;
    public const int SchemaDepth = 32;
    public const int Fields = 128;
    public const int Rules = 32;
    public const int NameLength = 128;
}

internal sealed class ValidationContext
{
    private readonly List<Segment> path = new();
    private List<ValidationIssue>? issues;
    private int steps;
    private bool exhausted;

    public bool Step()
    {
        if (exhausted)
        {
            return false;
        }

        if (++steps <= Limits.Steps)
        {
            return true;
        }

        AddMessage("Validation work limit exceeded.",
            "The request is too complex. Send fewer items or fields at a time.");
        exhausted = true;
        return false;
    }

    public Scope Enter(string name, bool attribute = false)
    {
        path.Add(new Segment(name, attribute, -1));
        return new Scope(this);
    }

    public Scope Enter(int index)
    {
        path.Add(new Segment(null, false, index));
        return new Scope(this);
    }

    public void Add(string message, string? requirement = null)
    {
        AddIssue(message, requirement ?? message, requirement is null ? ": " : " ");
    }

    public void AddMessage(string detail, string userMessage, bool includeField = false)
    {
        AddIssue(detail, userMessage, includeField ? ": " : null);
    }

    public void AddRule(string requirement, string? customMessage)
    {
        if (customMessage is null)
        {
            Add(Capitalize(requirement), requirement);
        }
        else
        {
            AddMessage(customMessage, customMessage);
        }
    }

    private void AddIssue(string message, string userMessage, string? fieldSeparator)
    {
        if (exhausted)
        {
            return;
        }

        issues ??= new List<ValidationIssue>();
        issues.Add(new ValidationIssue(Path(), message, UserPrefix(fieldSeparator), userMessage));

        if (issues.Count == Limits.Issues)
        {
            issues.Add(new ValidationIssue("$", "Issue limit exceeded.", string.Empty,
                "More errors were found. Fix the listed values and try again."));
            exhausted = true;
        }
    }

    public void ThrowIfInvalid()
    {
        if (issues is not null)
        {
            throw new ValidationException(issues);
        }
    }

    private string Path()
    {
        var result = new StringBuilder("$");

        foreach (Segment segment in path)
        {
            if (segment.Index >= 0)
            {
                result.Append('[').Append(segment.Index.ToString(CultureInfo.InvariantCulture));
                result.Append(']');
            }
            else if (segment.Name?.Contains('.', StringComparison.Ordinal) == true)
            {
                result.Append(segment.Attribute ? "[@\"" : "[\"");
                result.Append(segment.Name).Append("\"]");
            }
            else
            {
                result.Append(segment.Attribute ? ".@" : ".").Append(segment.Name);
            }
        }

        return result.ToString();
    }

    private string UserPrefix(string? fieldSeparator)
    {
        if (fieldSeparator is null)
        {
            return string.Empty;
        }

        for (int index = path.Count - 1; index >= 0; index--)
        {
            Segment segment = path[index];
            if (segment.Name is string name)
            {
                string label = segment.Attribute && name == "id" ? "ID" :
                    Capitalize(name.Replace('_', ' '));
                return label + fieldSeparator;
            }
        }

        return string.Empty;
    }

    private static string Capitalize(string text)
    {
        return char.ToUpperInvariant(text[0]) + text[1..];
    }

    private readonly record struct Segment(string? Name, bool Attribute, int Index);

    public readonly struct Scope : IDisposable
    {
        private readonly ValidationContext? context;

        public Scope(ValidationContext context)
        {
            this.context = context;
        }

        public void Dispose()
        {
            if (context is not null)
            {
                context.path.RemoveAt(context.path.Count - 1);
            }
        }
    }
}
