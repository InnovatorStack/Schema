using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace InnovatorSchema;

/// <summary>Validation issues that remain available after the source is released.</summary>
public class ValidationResult
{
    internal ValidationResult(IReadOnlyList<ValidationIssue> issues)
    {
        Issues = issues;
        Error = issues.Count == 0 ? null : new ValidationError(issues);
    }

    /// <summary>Whether validation completed without issues.</summary>
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsValid => Error is null;

    /// <summary>The structured error, or null when validation succeeded.</summary>
    public ValidationError? Error { get; }

    /// <summary>Failures in deterministic execution order.</summary>
    public IReadOnlyList<ValidationIssue> Issues { get; }

    /// <summary>Return the issues without copying them.</summary>
    public IReadOnlyList<ValidationIssue> GetIssues()
    {
        return Issues;
    }

    /// <summary>A readable summary, or an empty string for a valid result.</summary>
    public string GetErrorString()
    {
        return Error?.GetErrorString() ?? string.Empty;
    }
}

/// <summary>Validation issues and the resulting value or presence state.</summary>
public sealed class ValidationResult<T> : ValidationResult
{
    internal ValidationResult(IReadOnlyList<ValidationIssue> issues, ValidationValue<T> output)
        : base(issues)
    {
        if (IsValid && output.State == ValueState.Invalid)
        {
            throw new InvalidOperationException(
                "Successful validation must produce a defined state.");
        }

        OutputValue = output;
    }

    /// <summary>True when validation succeeded with a present, non-null output.</summary>
    public bool HasOutput => IsValid && OutputValue.HasValue;

    /// <summary>
    /// The output and its explicit presence state; invalid results expose no value.
    /// </summary>
    public ValidationValue<T> OutputValue { get; }

    /// <summary>
    /// The successful present output; check HasOutput for optional or nullable roots.
    /// </summary>
    public T Output
    {
        get
        {
            if (!HasOutput)
            {
                throw new InvalidOperationException("A successful present output is unavailable.");
            }

            return OutputValue.Value;
        }
    }
}
