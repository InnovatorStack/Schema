# Transform

Map a successful present value to a new output type.

## Signature

```csharp
Schema<TNext> schema.Transform<TNext>(Func<T, TNext> transform)
```

## Example

```csharp
var length = V.String().Transform(value => value.Length).Min(1);
```

## Behavior

Runs only when its source subtree has succeeded with a present value. Accepted missing/null states pass through without calling the transform. An unexpected null return produces NullNotAllowed. Caller exceptions propagate. The source input is not modified. Following rules operate on TNext.

## Related

[TransformNullable](/api/transform-nullable) · [Pipelines](/guides/pipelines)
