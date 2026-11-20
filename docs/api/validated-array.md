# ValidatedArray

Read validated collection positions in original order.

## Signature

```csharp
int values.Count
ValidationValue<T> values[int index]
IReadOnlyList<T> values.GetValues()
```

## Example

```csharp
var result = V.ValidateJson(V.Array(V.Integer()), "[1,2]");
long first = result.Output[0].Value;
```

## Behavior

Implements `IReadOnlyList<ValidationValue<T>>`. Indexes include accepted null positions. GetValues requires every entry present and returns a cached read-only view without copying the values. This wrapper is not a direct serialization model for nullable entries.
