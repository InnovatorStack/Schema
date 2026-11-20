# ValidationResult

Inspect issues and the successful output state.

## Signature

```csharp
bool result.IsValid
IReadOnlyList<ValidationIssue> result.Issues
string result.GetErrorString()
bool result.HasOutput
ValidationValue<T> result.OutputValue
T result.Output
```

## Example

```csharp
if (result.HasOutput)
{
    var value = result.Output;
}
```

## Behavior

IsValid means there are no issues. HasOutput additionally requires a present non-null output. Output throws when unavailable. OutputValue retains Missing/Null/Present/Invalid. Invalid results discard successful output. GetErrorString is an empty string on success and a bounded summary on failure.
