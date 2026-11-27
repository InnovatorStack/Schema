# Define a schema

`Item("Part")` requires a Part. Use `Item()` when the Item type can vary.
Add `.ID()` to require a valid 32-character Aras ID. `ParseInput` returns the original
ID text. Use `V.Guid()` for a field that contains an Aras ID. For an `add` that lets
Aras generate an ID, leave the ID rule off.

```csharp
var schema = V.Item()
    .Attribute("action", V.String().OneOf("add", "edit"))
    .Property("quantity", V.Integer().Min(1))
    .Property("weight", V.Double().Min(0.0))
    .Property("effective_date", V.Date().Optional());
```

Use the declared field's AML name in `Property` or `Attribute`. Other fields can remain in
the input. An Item or a one-Item `<AML>` wrapper can satisfy an Item schema. For multiple
Items, use [a collection schema](/examples/part-batch).
