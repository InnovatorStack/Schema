# Define a schema

`Item("Part")` requires a Part. Use `Item()` when the Item type can vary.
Add `.ID()` to require a supplied ID. Use `.ID(V.Guid())` to require the 32-character
Aras GUID format as well. For an `add` that lets Aras generate an ID, leave the rule off.

```csharp
var schema = V.Item()
    .Attribute("action", V.Enum("add", "edit"))
    .Property("quantity", V.Integer().Min(1))
    .Property("weight", V.Double().Min(0.0))
    .Property("effective_date", V.Date().Nullish());
```

Use the declared field's AML name in `Property` or `Attribute`. Other fields can remain in
the input. An Item or a one-Item `<AML>` wrapper can satisfy an Item schema. For multiple
Items, use [a collection schema](/examples/part-batch).
