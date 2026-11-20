# Parse

Return runtime output from the schema, or throw a validation error.

## Signature

```csharp
dynamic? V.Parse<T>(Schema<T> schema, string? input,
    InputFormat format = InputFormat.Json, ValidationOptions? options = null)
dynamic? V.Parse<T>(Schema<T> schema, object input, ValidationOptions? options = null)
```

## Example

```csharp
var schema = V.Object().Property("item_number", V.String().NotBlank());
var part = V.Parse(schema, "{\"item_number\":\"P-100\"}");
string number = part.item_number;
```

## Behavior

Objects become read-only runtime properties; nested schemas materialize recursively. Arrays become read-only lists. Missing optional fields read as null but remain absent from dictionary membership. Explicit null is retained. Invalid input throws ValidationException with structured issues. Caller exceptions and cancellation propagate unchanged. Item input is read through its public XML members.
