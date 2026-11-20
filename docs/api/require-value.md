# RequireValue

Require a present non-null output at this pipeline position.

## Signature

```csharp
Schema<T> schema.RequireValue()
```

## Example

```csharp
var code = V.String().Optional().RequireValue();
```

## Behavior

Unlike input-presence configuration, this is a pipeline stage. It runs after its source reader/pipeline and rejects Missing with Required or Null with NullNotAllowed. It can reject null deliberately introduced by TransformNullable. OutputPresence becomes Required.
