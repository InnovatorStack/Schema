# SafeParseResult

Inspect SafeParse output, its root presence and validation issues.

## Signature

```csharp
bool result.Success
dynamic? result.Data
ValueState result.DataState
ValidationError? result.Error
IReadOnlyList<ValidationIssue> result.GetIssues()
```

## Example

```csharp
var result = V.SafeParse(V.Integer(), "2");
if (result.Success)
{
    long count = result.Data;
}
```

## Behavior

Data can be null on a successful optional or nullable schema. DataState distinguishes Missing, Null and Present; invalid results expose Invalid. Error is null on success. Data is null on failure.
