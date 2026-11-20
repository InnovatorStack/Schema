# ValidationValue

Represent a value together with its presence or invalid state.

## Signature

```csharp
ValueState value.State
bool value.HasValue
bool value.IsMissing
bool value.IsNull
T value.Value
```

## Example

```csharp
if (result.OutputValue.HasValue)
{
    var value = result.OutputValue.Value;
}
```

## Behavior

ValueState is Missing, Null, Present or Invalid. Value throws unless the state is Present. The state avoids merging omitted source data with explicit null. Successful nullable roots can be Null; invalid results expose Invalid.
