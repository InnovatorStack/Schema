# Field

Create a property binding that retains its schema and output type.

## Signature

```csharp
Field<T> V.Field<T>(string name, Schema<T> schema)
```

## Example

```csharp
var number = V.Field("item_number", V.String().NotBlank());
var schema = V.Object(number);
```

## Behavior

Field names are exact and case-sensitive. The handle identifies this declaration, not every field with the same name. Use result.Output.Get(handle) for present values or Read(handle) to preserve missing/null state. Use Parse for automatic named output. Empty, whitespace-only or oversized names are rejected.

## Related

[ValidatedObject](/api/validated-object)
