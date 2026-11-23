# Schema

Assert the AML your Aras Innovator Method needs.

```csharp
var schema = V.Item("Part")
    .ID()
    .Attribute("action", V.Enum("edit", "add", "update"))
    .Property("item_number", V.String().MaxLength(64).Nullish())
    .Property("related_id", V.Item("Part").ID())
    .Property("weight", V.Integer().Min(0));

V.AssertInput(schema, this);
```

Declared fields require a nonblank value. Use `.Nullish()` to allow missing, null or blank
values. Extra fields are allowed. `V.Array(schema)` checks a whole collection.

[Installation](https://innovatorstack.github.io/Schema/guides/installation.html) ·
[API](https://innovatorstack.github.io/Schema/api/) ·
[Examples](https://innovatorstack.github.io/Schema/examples/part.html) ·
[Releases](https://github.com/InnovatorStack/Schema/releases)
