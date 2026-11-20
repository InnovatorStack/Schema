using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace InnovatorSchema;

/// <summary>The parsed data or validation error returned by SafeParse.</summary>
public sealed class SafeParseResult
{
    private readonly object? data;

    internal SafeParseResult(ValidationResult source, object? data, ValueState state)
    {
        Error = source.Error;
        this.data = data;
        DataState = state;
    }

    /// <summary>Whether parsing succeeded without validation issues.</summary>
    [MemberNotNullWhen(false, nameof(Error))]
    public bool Success => Error is null;

    /// <summary>The validation error, or null on success.</summary>
    public ValidationError? Error { get; }

    /// <summary>Missing, null or present on success; Invalid on failure.</summary>
    public ValueState DataState { get; }

    /// <summary>The parsed data on success; null on failure or an accepted null root.</summary>
    public dynamic? Data => data;

    /// <summary>The validation issues, or an empty list on success.</summary>
    public IReadOnlyList<ValidationIssue> Issues => Error?.Issues ?? Array.Empty<ValidationIssue>();

    /// <summary>Return validation issues without copying them.</summary>
    public IReadOnlyList<ValidationIssue> GetIssues()
    {
        return Issues;
    }

    /// <summary>The validation summary, or an empty string on success.</summary>
    public string GetErrorString()
    {
        return Error?.GetErrorString() ?? string.Empty;
    }
}
