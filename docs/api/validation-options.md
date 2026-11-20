# ValidationOptions

Configure one validation call's work bounds, issue mode and cancellation.

## Signature

```csharp
new ValidationOptions(
    ValidationMode mode = ValidationMode.AllIssues,
    int maximumInputLength = 1_048_576,
    int maximumDepth = 64,
    int maximumNodes = 100_000,
    int maximumCollectionLength = 10_000,
    int maximumIssues = 100,
    int maximumOperations = 100_000,
    CancellationToken cancellationToken = default)
```

## Example

```csharp
var options = new ValidationOptions(
    mode: ValidationMode.FirstIssue,
    maximumIssues: 20);
var result = V.ValidateJson(schema, json, options);
```

## Behavior

All bounds must be positive and within supported maxima. Options are immutable after construction. Cancellation is cooperative and propagates. FirstIssue stops after one failure; AllIssues collects independent failures within bounds. A terminal limit issue can replace the final retained issue when collection overflows.

## Related

[Limits and cancellation](/guides/limits)
