# Handle errors

`AssertInput` throws `ValidationException` with a readable message, such as
“Weight must be at least 3.” To return an Aras error Item with details, use `Message`
and `GetErrorString()`.

```csharp
var schema = V.Item("Part").Property("weight", V.Integer().Min(3));
var inn = this.getInnovator();

try
{
    V.AssertInput(schema, this);
}
catch (ValidationException error)
{
    var fault = inn.newError(error.Message);
    fault.setErrorDetail(error.GetErrorString());
    return fault;
}

return this;
```

- **Message** — Weight must be at least 3.
- **Details** — `$.weight: Must be at least 3.`

If the weight contains text like `heavy`, the message is “Weight must be a whole number.”
An invalid decimal shows “Weight must be a number. Use a period for decimals.”

Pass your own text with `.Min(3, "Enter a weight of 3 or more.")`.
`Issues` gives each failure's path and message. A failure on the second Item has a
path such as `$[1].weight`. The user message reads “Weight must be at least 3.”
