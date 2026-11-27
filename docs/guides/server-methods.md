# Server Methods

[Install Schema](/guides/installation), then pass the current Item to `AssertInput`.

<<< ../../examples/PartMethod.cs

Use an Item from another IOM call the same way. The validator reads the supplied Item in
place, so the Method can still use `getProperty`, `getPropertyItem` and `getID` afterward.

Use `ParseInput` when the Method needs the converted values. You can read a field
by name or with dot syntax. The second value is the Innovator instance from the Item.

<<< ../../examples/ParsePartMethod.cs

## SQL string values

Use `.SqlQuoted()` to return a SQL Server Unicode string literal. Pass the Method's
Aras `QuoteValue` function to `ParseInput`.

```csharp
var schema = V.Item().Property("item_number", V.String().MaxLength(64).SqlQuoted());
var (item, inn) = V.ParseInput(schema, this, CCO.DB.InnDatabase.QuoteValue);
return inn.newResult(item.item_number);
```

`O'Brien` becomes `N'O''Brien'`. Rules check the original text, then parsing escapes
it and adds the `N` prefix and outer quotes. Optional nulls remain `null`. `AssertInput` only
validates the input and takes no escaping function.
