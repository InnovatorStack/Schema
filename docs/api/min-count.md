# MinCount

Require a minimum number of collection positions.

## Signature

```csharp
Schema<ValidatedArray<T>> schema.MinCount(
    int count, string? message = null)
```

## Example

```csharp
var schema = V.Array(V.String()).MinCount(1);
```

## Behavior

Accepted null positions count as entries. A failure produces MinimumCount. Negative counts and incompatible count bounds in a segment are construction errors. This is a collection rule, separate from the source-wide MaximumCollectionLength resource limit. It remains available after Optional/Nullable when the current output type is still `ValidatedArray<T>`.
