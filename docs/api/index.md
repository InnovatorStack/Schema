# API

Use `using InnovatorSchema;` and `using V = InnovatorSchema.Validator;`.

## Item schema

```csharp
var part = V.Item("Part")
    .ID(V.Guid())
    .Attribute("action", V.Enum("edit", "add", "update"))
    .Property("item_number", V.String().Max(64))
    .Property("related_id", V.Item().ID().Nullish());
```

`Item("Part")` requires the exact type. `Item()` accepts any Item type.
`ID()` requires a nonblank ID. `ID(V.Guid())` also checks its format. A nested Item's
`ID()` always checks that Item's own ID. `Attribute` and `Property` add a field to a
new schema. Other fields can remain in the input.

## Scalars

| Call | Accepts |
| --- | --- |
| `V.String()` | Nonblank text |
| `V.Enum("add", "edit")` | One of the strings, with exact casing |
| `V.Integer()` | Signed 64-bit integer text |
| `V.Double()` | Finite floating-point text with a period as the decimal separator |
| `V.Date()` | `yyyy-MM-dd` or `yyyy-MM-ddTHH:mm:ss` |
| `V.Guid()` | Exactly 32 hex characters in either case, with at least one nonzero digit |

`Min` and `Max` set inclusive bounds on values, string length, or the number of
Items in a collection. Pass a message as the second argument to customize a scalar
bound's error.
`Nullish()` permits missing, null, empty or whitespace values on a declared field.

```csharp
var count = V.Integer().Min(0, "Enter a nonnegative count.").Max(100);
var date = V.Date().Nullish();
var name = V.String().Min(2).Max(64);
```

## Collections

```csharp
var batch = V.Array(V.Item("Part").ID()).Min(1).Max(50);
```

A single supplied Item is a collection of one for `V.Array`. An empty AML collection or
IOM “No Items Found” result has zero rows.

## AssertInput

```csharp
var schema = V.Item("Part").ID();
var aml = "<Item type='Part' id='783BA98DA5D04BF9AF4B1760826221F0' />";
V.AssertInput(schema, aml);
```

`AssertInput` accepts either an Item schema or a collection schema, and either AML text
or an IOM Item. It returns normally on success and throws `ValidationException` on failure.
The exception's `Message` is ready to display. Use `GetErrorString()` for AML paths or
`Issues` and `GetIssues()` to inspect failures individually.

## ParseInput

```csharp
var schema = V.Item("Part")
    .ID(V.Guid())
    .Property("item_number", V.String().Max(64))
    .Property("weight", V.Integer().Min(0))
    .Property("cost", V.Double().Nullish());

var aml = "<Item type='Part' id='783BA98DA5D04BF9AF4B1760826221F0'>" +
    "<item_number>P-100</item_number><weight>4</weight></Item>";
var item = V.ParseInput(schema, aml);
var itemNo = item["item_number"]; // string
var weight = item["weight"]; // long
var cost = item.cost; // null
```

`ParseInput` accepts AML text or an IOM Item. It returns the validated fields
declared by the schema. Invalid input throws the same `ValidationException` as
`AssertInput`.

Use `item.item_number` or `item["item_number"]` for a property, and `item["@action"]`
for a declared attribute. Use brackets for names such as `part-number`. Nested Items
use the same access, as in `item.related_id.item_number`. An ID rule supplies
`item.id` and `item["@id"]`
when the corresponding property or attribute has no explicit rule. Plain `.ID()`
yields text and `.ID(V.Guid())` yields a `Guid`.

The result resolves field names at runtime. String and Enum yield `string`, Integer
yields `long`, Double yields `double`, Date yields `DateTime`, and Guid yields
`Guid`. A missing, null, or blank value accepted by `.Nullish()` yields `null`.
The result is a read-only snapshot of those values.

Pass an array schema to get an indexed list of parsed Items.

```csharp
var schema = V.Array(V.Item("Part").Property("weight", V.Integer())).Min(1).Max(20);
var parts = V.ParseInput(schema, "<AML><Item type='Part'><weight>4</weight></Item></AML>");
long weight = parts[0].weight;
```

See the [BOM example](/examples/part-batch#parse-bom-rows) for a Method that reads a batch
with nested Parts.

## DLL version

```csharp
var version = global::InnovatorSchema.SchemaInfo.GetVersion();
```

`GetVersion()` reads the version of the running DLL, including build metadata when present.
