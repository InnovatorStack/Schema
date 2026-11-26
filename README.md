# Schema

Assert the AML your Aras Innovator Method needs.

```csharp
var schema = V.Item("Part")
    .ID(V.Guid())
    .Attribute("action", V.Enum("edit", "add", "update"))
    .Property("item_number", V.String().Max(64).Nullish())
    .Property("related_id", V.Item("Part").ID())
    .Property("weight", V.Integer().Min(0));

V.AssertInput(schema, this);
```

Declared fields require a nonblank value. Use `.Nullish()` to allow missing, null or blank
values. Extra fields are allowed. `V.Array(schema)` checks a whole collection.

Use `V.ParseInput(schema, this)` to read validated fields as `item.item_number` or
`item["item_number"]`.

[Installation](https://innovatorstack.github.io/Schema/guides/installation.html) ·
[API](https://innovatorstack.github.io/Schema/api/) ·
[Examples](https://innovatorstack.github.io/Schema/examples/part.html) ·
[Releases](https://github.com/InnovatorStack/Schema/releases)
