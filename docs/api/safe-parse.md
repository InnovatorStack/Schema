# SafeParse

Return parsed data or a structured validation error.

## Signature

```csharp
SafeParseResult V.SafeParse<T>(Schema<T> schema, string? input,
    InputFormat format = InputFormat.Json, ValidationOptions? options = null)
SafeParseResult V.SafeParse<T>(Schema<T> schema, object input,
    ValidationOptions? options = null)
```

## Example

```csharp
var schema = V.Object().Property("name", V.String().NotBlank());
var result = V.SafeParse(schema, "{}");
if (!result.Success)
{
    var issues = result.Error.GetIssues();
    string message = result.Error.GetErrorString();
}
```

## Behavior

Validation failures return `Success = false`, `Data = null` and a `ValidationError` in `Error`.
Successful calls return `Success = true`, the parsed `Data` and `Error = null`. `DataState`
distinguishes a valid null or omitted root from a present value.

Invalid arguments, cancellation and exceptions from caller predicates or transforms propagate.
Results contain plain data and errors. Aras error Items can be created explicitly by the caller.
