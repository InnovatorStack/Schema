using System;
using System.Threading;

namespace InnovatorSchema;

/// <summary>Issue collection, cancellation and per-call resource limits.</summary>
public sealed class ValidationOptions
{
    /// <summary>
    /// Create bounded execution options. Cancellation is cooperative between operations.
    /// </summary>
    public ValidationOptions(
        ValidationMode mode = ValidationMode.AllIssues,
        int maximumInputLength = 1_048_576,
        int maximumDepth = 64,
        int maximumNodes = 100_000,
        int maximumCollectionLength = 10_000,
        int maximumIssues = 100,
        int maximumOperations = 100_000,
        CancellationToken cancellationToken = default)
    {
        if (mode != ValidationMode.AllIssues && mode != ValidationMode.FirstIssue)
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        Mode = mode;
        MaximumInputLength = Bounded(maximumInputLength, 16_777_216, nameof(maximumInputLength));
        MaximumDepth = Bounded(maximumDepth, 128, nameof(maximumDepth));
        MaximumNodes = Bounded(maximumNodes, 1_000_000, nameof(maximumNodes));
        MaximumCollectionLength = Bounded(
            maximumCollectionLength, 100_000, nameof(maximumCollectionLength));
        MaximumIssues = Bounded(maximumIssues, 1000, nameof(maximumIssues));
        MaximumOperations = Bounded(maximumOperations, 10_000_000, nameof(maximumOperations));
        CancellationToken = cancellationToken;
    }

    /// <summary>Whether to collect issues or stop after the first failure.</summary>
    public ValidationMode Mode { get; }
    /// <summary>Maximum source text length in UTF-16 code units.</summary>
    public int MaximumInputLength { get; }
    /// <summary>Maximum input nesting depth.</summary>
    public int MaximumDepth { get; }
    /// <summary>Maximum visited source nodes.</summary>
    public int MaximumNodes { get; }
    /// <summary>Maximum entries in any supplied collection.</summary>
    public int MaximumCollectionLength { get; }
    /// <summary>Maximum retained issues, including a terminal truncation marker.</summary>
    public int MaximumIssues { get; }
    /// <summary>Maximum execution node and rule operations.</summary>
    public int MaximumOperations { get; }
    /// <summary>Cancellation checked between bounded operations.</summary>
    public CancellationToken CancellationToken { get; }

    private static int Bounded(int value, int maximum, string parameter)
    {
        if (value < 1 || value > maximum)
        {
            throw new ArgumentOutOfRangeException(parameter);
        }

        return value;
    }
}

/// <summary>How ordinary independent validation failures are collected.</summary>
public enum ValidationMode
{
    /// <summary>Collect independent failures up to the configured issue limit.</summary>
    AllIssues,
    /// <summary>Stop after the first failure.</summary>
    FirstIssue
}
