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

        Add("Validation work limit exceeded.");
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

    public void Add(string message)
    {
        if (exhausted)
        {
            return;
        }

        issues ??= new List<ValidationIssue>();
        issues.Add(new ValidationIssue(Path(), message));

        if (issues.Count == Limits.Issues)
        {
            issues.Add(new ValidationIssue("$", "Issue limit exceeded."));
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
