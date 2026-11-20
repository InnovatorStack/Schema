# ValidateAml

Validate AML text against a definition.

## Signature

```csharp
ValidationResult<T> V.ValidateAml<T>(
    Schema<T> schema, string? aml, ValidationOptions? options = null)
```

## Example

```csharp
var result = V.ValidateAml(schema, aml);

if (!result.IsValid)
{
    string summary = result.GetErrorString();
}
```

## Behavior

Absent text is checked against root presence; malformed source produces an input issue. Invalid results expose no successful output. Caller callbacks and cancellation can throw. Options are per call. A single Item schema requires exactly one Item. An array schema reads every Item, including an empty AML collection when its rules allow it. Aras property null markers and expanded Item semantics apply. No database access is performed.

## Related

[ValidationResult](/api/validation-result) · [ValidationOptions](/api/validation-options)
