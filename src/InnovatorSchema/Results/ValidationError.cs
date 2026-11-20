using System;
using System.Collections.Generic;
using System.Text;

namespace InnovatorSchema;

/// <summary>Validation issues with a readable summary.</summary>
public sealed class ValidationError
{
    private string? summary;

    internal ValidationError(IReadOnlyList<ValidationIssue> issues)
    {
        if (issues.Count == 0)
        {
            throw new ArgumentException("A validation error requires at least one issue.");
        }

        Issues = issues;
    }

    /// <summary>Issues in validation order.</summary>
    public IReadOnlyList<ValidationIssue> Issues { get; }

    /// <summary>Return the issues without copying them.</summary>
    public IReadOnlyList<ValidationIssue> GetIssues()
    {
        return Issues;
    }

    /// <summary>Return a summary bounded to 8,192 characters.</summary>
    public string GetErrorString()
    {
        return summary ??= Summarize(Issues);
    }

    private static string Summarize(IReadOnlyList<ValidationIssue> issues)
    {
        var result = new StringBuilder("Schema validation failed.\nProblems:");

        foreach (ValidationIssue issue in issues)
        {
            string line = "\n- " + issue.Path + ": " + issue.Message;
            const int maximum = 8192;
            const string marker = " [truncated]";

            if (result.Length + line.Length > maximum)
            {
                int available = maximum - marker.Length - result.Length;

                if (available > 0)
                {
                    result.Append(line.AsSpan(0, Math.Min(available, line.Length)));
                }
                else
                {
                    result.Length = maximum - marker.Length;
                }

                if (result.Length > 0 && char.IsHighSurrogate(result[result.Length - 1]))
                {
                    result.Length--;
                }

                result.Append(marker);
                break;
            }

            result.Append(line);
        }

        return result.ToString();
    }
}

/// <summary>A Parse call failed validation; Error contains its issues.</summary>
public sealed class ValidationException : Exception
{
    /// <summary>Create an exception for a validation error.</summary>
    public ValidationException(ValidationError error) : base(MessageFor(error))
    {
        Error = error;
    }

    /// <summary>The validation error that caused this exception.</summary>
    public ValidationError Error { get; }

    /// <summary>The same issues exposed by Error.</summary>
    public IReadOnlyList<ValidationIssue> Issues => Error.Issues;

    /// <summary>Return the validation issues without copying them.</summary>
    public IReadOnlyList<ValidationIssue> GetIssues()
    {
        return Error.GetIssues();
    }

    /// <summary>Return the same readable summary exposed by Message.</summary>
    public string GetErrorString()
    {
        return Error.GetErrorString();
    }

    private static string MessageFor(ValidationError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return error.GetErrorString();
    }
}
