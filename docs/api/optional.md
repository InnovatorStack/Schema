# Optional

Permit omitted source input.

## Signature

```csharp
Schema<T> schema.Optional()
```

## Example

```csharp
var weight = V.DecimalText().Min(0m).Optional();
```

## Behavior

A present value still runs its reader and checks. Explicit null is not omission; add Nullable to accept null. Accepted missing states skip present-value checks and transforms. Optional configures input presence regardless of its position in the fluent expression. It does not provide a default.

## Related

[Nullable](/api/nullable) · [Missing and null](/guides/presence)
