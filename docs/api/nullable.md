# Nullable

Permit explicit null source input.

## Signature

```csharp
Schema<T> schema.Nullable()
```

## Example

```csharp
var note = V.String().Optional().Nullable();
```

## Behavior

Accepted null remains an explicit state and skips present-value checks/transforms. Omission still requires Optional. In AML/IOM, selected property is_null markers follow Aras null grammar. Ordinary XML attributes do not introduce null. Nullable roots can succeed without a present Output.
