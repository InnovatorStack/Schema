# Schema

Assert the AML your Aras Innovator Method needs.

```csharp
var schema = V.Item("Part").ID()
    .Property("item_number", V.String().MaxLength(64))
    .Property("weight", V.Double().Min(0.0).Optional());

V.AssertInput(schema, this);
```

Describe the fields your Method needs. Then use the original Item and IOM getters.
[Validate a Part](/examples/part) or [check every Item in a collection](/examples/part-batch).

[Installation](/guides/installation) · [API](/api/)
