# API

Use `using InnovatorSchema;` and `using V = InnovatorSchema.Validator;`.

## Item schema

```csharp
var part = V.Item("Part")
    .ID()
    .Attribute("action", V.String().OneOf("edit", "add", "update"))
    .Property("item_number", V.String().MaxLength(64))
    .Property("related_id", V.Item().ID().Optional());
```

`Item("Part")` requires the exact type. `Item()` accepts any Item type.
`ID()` requires a valid Aras ID and returns its original text when parsing.
Use `ID(V.Guid())` to parse it as a `Guid`. A nested Item's `ID()` checks that Item's
own ID. `Attribute` and `Property` add a field to a new schema. Extra fields are allowed.

## Scalars

| Call | Accepts |
| --- | --- |
| `V.String()` | Nonblank text |
| `V.Bool()` | `0` for `false` or `1` for `true` |
| `V.Integer()` | Signed 64-bit integer text |
| `V.Double()` | Finite floating-point text with a period as the decimal separator |
| `V.Date()` | `yyyy-MM-dd` or `yyyy-MM-ddTHH:mm:ss` |
| `V.Guid()` | Exactly 32 hex characters in either case, with at least one nonzero digit |

`Min` and `Max` set inclusive bounds on values and collection sizes. Use
`MinLength` or `MaxLength` for a one-sided string length, `Length(20)` for an exact
length, and `Length(1, 250)` for an inclusive range. Pass a message as the last
argument to customize a scalar bound's error.

`Optional()` permits missing and null values. Optional strings preserve `""`.
`Default(value)` supplies a validated fallback for a missing or null value.

`OneOf(...)` compares the parsed scalar with the supplied choices. String choices
match exactly by default. Add `.Insensitive()` after `OneOf(...)` to compare strings
without regard to case. Parsing preserves the original casing.

```csharp
var count = V.Integer().Min(0, "Enter a nonnegative count.").Max(100);
var date = V.Date().Optional();
var name = V.String().Length(2, 64);
var type = V.String().OneOf("user", "identity").Insensitive().Default("user");
var priority = V.Integer().OneOf(1, 3, 5);
var isTest = V.Bool().Default(false);
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
    .Property("item_number", V.String().MaxLength(64))
    .Property("weight", V.Integer().Min(0))
    .Property("cost", V.Double().Optional());

var aml = "<Item type='Part' id='783BA98DA5D04BF9AF4B1760826221F0'>" +
    "<item_number>P-100</item_number><weight>4</weight></Item>";
var (item, _) = V.ParseInput(schema, aml);
var itemNo = item["item_number"]; // string
var weight = item["weight"]; // long
var cost = item.cost; // null
```

`ParseInput` returns `(Input, Innovator)`. `Input` contains the validated fields.
For an IOM Item, `Innovator` comes from `getInnovator()`. For AML text, it is `null`.
Invalid input throws the same `ValidationException` as `AssertInput`.

Use `item.item_number` or `item["item_number"]` for a property, and `item["@action"]`
for a declared attribute. Use brackets for names such as `part-number`. Nested Items
use the same access, as in `item.related_id.item_number`. An ID rule supplies
`item.id` and `item["@id"]`
when the corresponding property or attribute has no explicit rule. Plain `.ID()`
yields text and `.ID(V.Guid())` yields a `Guid`.

The result resolves field names at runtime. String yields `string`, Bool yields `bool`,
Integer yields `long`, Double yields `double`, Date yields `DateTime`, and Guid yields
`Guid`. An omitted or explicitly null optional field yields `null`.
An empty optional string yields `""`.
The result is a read-only snapshot of those values.

Pass an array schema to get an indexed list of parsed Items.

```csharp
var schema = V.Array(V.Item("Part").Property("weight", V.Integer())).Min(1).Max(20);
var (parts, _) = V.ParseInput(schema, "<AML><Item type='Part'><weight>4</weight></Item></AML>");
long weight = parts[0].weight;
```

See the [BOM example](/examples/part-batch#parse-bom-rows) for a Method that reads a batch
with nested Parts.

Use [`SqlQuoted()`](/guides/server-methods#sql-string-values) on a string field to get a
SQL string literal after validation.

## DLL version

```csharp
var version = global::InnovatorSchema.SchemaInfo.GetVersion();
```

`GetVersion()` reads the version of the running DLL, including build metadata when present.
