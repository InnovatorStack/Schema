using System;
using System.Collections.Generic;
using System.Text;

namespace InnovatorSchema;

/// <summary>One failed assertion at a location in the supplied AML.</summary>
public sealed class ValidationIssue
{
    internal ValidationIssue(string path, string message)
    {
        Path = path;
        Message = message;
    }

    /// <summary>The location, such as $.weight or $[2].related_id.@id.</summary>
    public string Path { get; }

    /// <summary>Why this value failed.</summary>
    public string Message { get; }
}

/// <summary>The supplied AML did not satisfy its schema.</summary>
public sealed class ValidationException : Exception
{
    internal ValidationException(List<ValidationIssue> issues) : base(Format(issues))
    {
        Issues = issues.AsReadOnly();
    }

    /// <summary>Issues in validation order.</summary>
    public IReadOnlyList<ValidationIssue> Issues { get; }

    /// <summary>Return the issues for individual inspection.</summary>
    public IReadOnlyList<ValidationIssue> GetIssues()
    {
        return Issues;
    }

    /// <summary>Return a readable summary for display.</summary>
    public string GetErrorString()
    {
        return Message;
    }

    private static string Format(List<ValidationIssue> issues)
    {
        const int maxLength = 8192;
        var text = new StringBuilder("Schema validation failed.");

        foreach (ValidationIssue issue in issues)
        {
            if ((long)text.Length + issue.Path.Length + issue.Message.Length + 5 > maxLength - 15)
            {
                text.Append("\n[Truncated]");
                break;
            }

            text.Append("\n- ").Append(issue.Path).Append(": ").Append(issue.Message);
        }

        return text.ToString();
    }
}
