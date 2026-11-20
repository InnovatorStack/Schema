# Parsed output

`Parse` creates an object from your schema. Declared fields become runtime properties,
including nested objects and arrays. You do not declare an output class.

```csharp
var schema = V.Object()
    .Property("item_number", V.String().NotBlank())
    .Property("weight", V.DecimalText().Min(0m).Optional());

var part = V.Parse(schema, "{\"item_number\":\"P-100\",\"weight\":\"2.5\"}");
string number = part.item_number;
decimal weight = part.weight;
```

The output uses the schema's converted values. `weight` is a decimal, not its source text.
`Parse` throws `ValidationException` on invalid data. Use `SafeParse` to handle failures as results.

## SafeParse

```csharp
var result = V.SafeParse(schema, json);

if (!result.Success)
{
    string message = result.Error.GetErrorString();
    var issues = result.Error.GetIssues();
}
else
{
    var part = result.Data;
    string number = part.item_number;
}
```

`Data` is null on failure and `Error` contains the issues. A valid nullable
root returns null. `DataState` distinguishes missing, null, present and invalid roots.

## Nested data

Object fields retain their declared names. Arrays are read-only lists with `Count` and an
integer index. For example: `order.lines[0].quantity`. Null entries keep their positions.
Additional source properties are excluded from parsed output.

Attributes use string indexes such as `part["@action"]`. Relationships are grouped by type,
for example `part.Relationships["Part BOM"][0].quantity`.

## Optional fields

An omitted optional field reads as null. `part.ContainsKey("weight")` is false when the
field was omitted and true for explicit null. Undeclared names fail rather than silently
returning null. Use string indexes for names that are not C# identifiers: `part["part-number"]`.

## Serialization and ownership

Parsed objects and lists serialize as ordinary JSON objects and arrays. Omitted fields stay
omitted; explicit null remains null. Data is read-only and independent of the source DOM.
Values explicitly returned by a custom `Transform` are preserved as supplied by that callback.

## Typed validation views

`Validate` returns `ValidationResult<T>` without materializing runtime properties. Advanced
code can retain `Field<T>` handles and read `result.Output.Get(field)` or `Read(field)`.
`ValidatedArray<T>.GetValues()` returns a cached read-only view and requires present entries.
Use `Parse` or `SafeParse` when you want named properties and ordinary JSON serialization.
