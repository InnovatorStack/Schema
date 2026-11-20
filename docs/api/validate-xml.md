# ValidateXml

Validate ordinary XML text against a definition.

## Signature

```csharp
ValidationResult<T> V.ValidateXml<T>(
    Schema<T> schema, string? xml, ValidationOptions? options = null)
```

## Example

```csharp
var result = V.ValidateXml(schema, xml);

if (!result.IsValid)
{
    string summary = result.GetErrorString();
}
```

## Behavior

Absent text is checked against root presence; malformed source produces an input issue. Invalid results expose no successful output. Caller callbacks and cancellation can throw. Options are per call. Generic XML preserves literal names/attributes and has no Aras is_null or automatic Item-unwrapping semantics. DTDs are prohibited.

## Related

[ValidationResult](/api/validation-result) · [ValidationOptions](/api/validation-options)
