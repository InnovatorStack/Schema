# Object

Declare named fields in an object shape.

## Signature

```csharp
ObjectSchema V.Object(params Field[] fields)
```

## Example

```csharp
var schema = V.Object()
    .Property("item_number", V.String().NotBlank())
    .Property("quantity", V.Decimal().Min(0.001m));
```

## Behavior

Objects allow unknown properties by default. Duplicate source properties fail independently of the unknown-property policy. Field declarations must be non-null and unique by binding identity. Add fields before whole-object checks or transforms. Validate returns a ValidatedObject; Parse materializes its declared fields.

## Related

[Property](/api/property) · [UnknownProperties](/api/unknown-properties) · [Field](/api/field)
