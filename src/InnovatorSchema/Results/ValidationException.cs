using System;
using System.Collections.Generic;
using System.Text;

namespace InnovatorSchema;

/// <summary>One failed assertion at a location in the supplied AML.</summary>
public sealed class ValidationIssue
{
    internal ValidationIssue(string path, string message, string userPrefix, string userMessage)
    {
        Path = path;
        Message = message;
        UserPrefix = userPrefix;
        UserMessage = userMessage;
    }

    /// <summary>The location, such as $.weight or $[2].related_id.@id.</summary>
    public string Path { get; }

    /// <summary>Why this value failed.</summary>
    public string Message { get; }

    internal string UserPrefix { get; }
    internal string UserMessage { get; }
}

/// <summary>The supplied AML did not satisfy its schema.</summary>
public sealed class ValidationException : Exception
{
    internal ValidationException(List<ValidationIssue> issues)
        : base(Format(issues, userFacing: true))
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

    /// <summary>Return the failure messages with their AML paths.</summary>
    public string GetErrorString()
    {
        return Format(Issues, userFacing: false);
    }

    private static string Format(IReadOnlyList<ValidationIssue> issues, bool userFacing)
    {
        const int maxLength = 8192;
        var text = new StringBuilder(userFacing ? string.Empty : "Schema validation failed.");

        foreach (ValidationIssue issue in issues)
        {
            string prefix = userFacing ? issue.UserPrefix : issue.Path;
            string message = userFacing ? issue.UserMessage : issue.Message;

            if ((long)text.Length + prefix.Length + message.Length + 5 > maxLength - 15)
            {
                text.Append(text.Length == 0 ? "[Truncated]" : "\n[Truncated]");
                break;
            }

            if (text.Length > 0)
            {
                text.Append('\n');
            }

            if (userFacing)
            {
                text.Append(prefix).Append(message);
            }
            else
            {
                text.Append("- ").Append(prefix).Append(": ").Append(message);
            }
        }

        return text.ToString();
    }
}
