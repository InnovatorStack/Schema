# And

Compose reusable value checks in declaration order.

## Signature

```csharp
Rule<T> Rule<T>.And(Rule<T> other)
```

## Example

```csharp
var bounds = V.Min(0.001m).And(V.Max(1000m));
var quantity = V.DecimalText().Check(bounds);
```

## Behavior

Returns a new immutable rule group. The original rules remain unchanged. Incompatible bound/length/count checks in the combined segment are rejected. Composition does not change source representation or create another schema reader.
