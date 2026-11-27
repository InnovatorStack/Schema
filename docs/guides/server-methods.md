# Server Methods

[Install Schema](/guides/installation), then pass the current Item to `AssertInput`.

<<< ../../examples/PartMethod.cs

Use an Item from another IOM call the same way. The validator reads the supplied Item in
place, so the Method can still use `getProperty`, `getPropertyItem` and `getID` afterward.

Use `ParseInput` when the Method needs the converted values. You can read a field
by name or with dot syntax. The second value is the Innovator instance from the Item.

<<< ../../examples/ParsePartMethod.cs

## SQL string values

Use `.SqlQuoted()` to escape apostrophes and surround the value with single quotes.
Use `.SqlQuoted(false)` to return only the escaped text.

```csharp
var schema = V.Item()
    .ID()
    .Attribute("idList", V.Array(V.Guid().SqlQuoted(), ","))
    .Property("item_number", V.String().SqlQuoted())
    .Property("type", V.String().SqlQuoted(false));

var (item, inn) = V.ParseInput(schema, this);
string[] ids = item["@idList"];
string itemNumber = item.item_number;
string type = item.type;
```

`O'Brien` becomes `'O''Brien'` with `.SqlQuoted()` and `O''Brien` with
`.SqlQuoted(false)`. In the second form, supply the surrounding SQL quotes yourself.
Rules check the original text. Optional nulls remain `null`. Arrays quote each element
individually and return the array.
