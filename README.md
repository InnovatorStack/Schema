# Schema

Assert the AML your Aras Innovator Method needs.

```csharp
var schema = V.Item("Part")
    .ID()
    .Attribute("action", V.String().Enum("edit", "add", "update"))
    .Property("item_number", V.String().MaxLength(64).Optional())
    .Property("related_id", V.Item("Part").ID())
    .Property("weight", V.Integer().Min(0));

V.AssertInput(schema, this);
```

Declared fields are required. `.Optional()` accepts missing or null values and preserves
empty strings. `V.Array(schema)` checks a whole collection. Extra fields are allowed.

Use `V.ParseInput(schema, this)` to read validated fields as `item.item_number` or
`item["item_number"]`.

[Installation](https://innovatorstack.github.io/Schema/guides/installation.html) ·
[API](https://innovatorstack.github.io/Schema/api/) ·
[Examples](https://innovatorstack.github.io/Schema/examples/part.html) ·
[Releases](https://github.com/InnovatorStack/Schema/releases)
