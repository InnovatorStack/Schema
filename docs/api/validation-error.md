# ValidationError

Read a validation failure as structured issues or a message.

## Signature

```csharp
IReadOnlyList<ValidationIssue> error.Issues
IReadOnlyList<ValidationIssue> error.GetIssues()
string error.GetErrorString()
```

## Example

```csharp
var result = V.SafeParse(V.String().NotBlank(), "\"\"");
if (!result.Success)
{
    string message = result.Error.GetErrorString();
    var first = result.Error.GetIssues()[0];
}
```

## Behavior

Contains at least one issue. The list is read-only, ordered and returned without copying. The readable summary is bounded to 8,192 characters. AllIssues is the default; FirstIssue and issue limits can reduce the list. The same error instance is shared by result wrappers.
