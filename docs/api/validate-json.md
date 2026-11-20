# ValidateJson

Validate JSON text against a definition.

## Signature

```csharp
ValidationResult<T> V.ValidateJson<T>(
    Schema<T> schema, string? json, ValidationOptions? options = null)
```

## Example

```csharp
var result = V.ValidateJson(schema, json);

if (!result.IsValid)
{
    string summary = result.GetErrorString();
}
```

## Behavior

Absent text is checked against root presence; malformed source produces an input issue. Invalid results expose no successful output. Caller callbacks and cancellation can throw. Options are per call. JSON comments, trailing commas, duplicate keys and malformed Unicode are rejected.

## Related

[ValidationResult](/api/validation-result) · [ValidationOptions](/api/validation-options)
