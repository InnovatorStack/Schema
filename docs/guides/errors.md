# Handle errors

`AssertInput` throws `ValidationException` for invalid input. `GetErrorString()` gives
a message for people. `Issues` gives the path and message for each failure.

```csharp
var schema = V.Item("Part").ID();

try
{
    V.AssertInput(schema, this);
}
catch (ValidationException error)
{
    return this.getInnovator().newError(error.GetErrorString());
}

return this;
```

For example, a problem with the second Item's weight appears at `$[1].weight`.
