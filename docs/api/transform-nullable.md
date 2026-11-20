# TransformNullable

Explicitly permit a transform to produce null.

## Signature

```csharp
Schema<TNext> schema.TransformNullable<TNext>(
    Func<T, TNext?> transform) where TNext : class
Schema<TNext> schema.TransformNullable<TNext>(
    Func<T, TNext?> transform) where TNext : struct
```

## Example

```csharp
var code = V.String().TransformNullable<string>(value =>
    value.Length == 0 ? null : value);
```

## Behavior

Reference and nullable-value callbacks have separate overloads. A null output becomes `ValidationValue<TNext>.Null` rather than a present value. OutputPresence includes AllowNull. Input null is a separate policy controlled by Nullable. Missing/null input skips the callback. Use RequireValue to reject a nullable output later.
