# Validate

Validate input and retain typed validation views and structured issues.

## Signature

```csharp
ValidationResult<T> V.Validate<T>(Schema<T> schema, string? input,
    InputFormat format = InputFormat.Json, ValidationOptions? options = null)
ValidationResult<T> V.Validate<T>(Schema<T> schema, object input,
    ValidationOptions? options = null)
```

## Example

```csharp
var schema = V.Object().Property("name", V.String().NotBlank());
var result = V.Validate(schema, "{\"name\":\"Bracket\"}");
```

## Behavior

Text defaults to JSON; select XML or AML with `InputFormat`. Object input accepts an Aras
Item, Item selection, AML `XmlElement`, `XmlDocument` or selected `XmlNodeList`. Ordinary
validation failures become issues. Results hold no Innovator context.
