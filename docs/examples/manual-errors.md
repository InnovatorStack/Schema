# Handle errors yourself

Use a plain text entry point when application code owns error handling. This call has no
Innovator argument and creates no Aras error Item.

```csharp
var schema = V.Object()
    .Property("item_number", V.String().NotBlank())
    .Property("quantity", V.Decimal().Min(0.001m));

var result = V.ValidateJson(schema, payload);

if (!result.IsValid)
{
    string summary = result.GetErrorString();

    foreach (var issue in result.Issues)
    {
        string path = issue.Path.ToString();
        string message = issue.Message;
    }
}
```

Choose your own response format, logging or application exception. `Issues` preserves stable
codes and paths instead of requiring text parsing. Do not catch every exception as invalid
input: caller programming errors and cancellation propagate deliberately.
