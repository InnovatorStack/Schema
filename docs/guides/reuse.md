# Reuse schemas

An operation such as `.Property()` or `.Min()` returns a new schema. Keep a reusable
definition for the common fields and extend it for each Method.

```csharp
var part = V.Item("Part").ID()
    .Property("item_number", V.String().MaxLength(64));

var update = part.Attribute("action", V.String().OneOf("edit", "update"));
var lot = V.Array(part).Max(50);
```
