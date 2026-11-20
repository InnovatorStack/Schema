# ValidationException

Handle validation failures from Parse.

## Signature

```csharp
ValidationError exception.Error
IReadOnlyList<ValidationIssue> exception.GetIssues()
string exception.GetErrorString()
```

## Example

```csharp
try
{
    var value = V.Parse(V.Integer().Min(1L), "0");
}
catch (ValidationException error)
{
    var issues = error.GetIssues();
    string message = error.GetErrorString();
}
```

## Behavior

A sealed exception containing the full ValidationError. Message matches GetErrorString. Parse throws it for validation failures; SafeParse returns a ValidationError instead. Invalid arguments, cancellation and callback exceptions retain their own types.
