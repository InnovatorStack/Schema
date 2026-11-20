# MaxCount

Limit the number of collection positions.

## Signature

```csharp
Schema<ValidatedArray<T>> schema.MaxCount(
    int count, string? message = null)
```

## Example

```csharp
var schema = V.Array(V.String()).MaxCount(1);
```

## Behavior

Accepted null positions count as entries. A failure produces MaximumCount. Negative counts and incompatible count bounds in a segment are construction errors. This is a collection rule, separate from the source-wide MaximumCollectionLength resource limit. It remains available after Optional/Nullable when the current output type is still `ValidatedArray<T>`.
