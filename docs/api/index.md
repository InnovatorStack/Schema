# API

Use `using InnovatorSchema;` and `using V = InnovatorSchema.Validator;`.

## Item schema

```csharp
var part = V.Item("Part")
    .ID(V.Guid())
    .Attribute("action", V.Enum("edit", "add", "update"))
    .Property("item_number", V.String().MaxLength(64))
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

`Min` and `Max` set inclusive bounds. `MinLength` and `MaxLength` set string length
bounds. Pass a message as the second argument to customize a bound's error.
`Nullish()` permits missing, null, empty or whitespace values on a declared field.

```csharp
var count = V.Integer().Min(0, "Enter a nonnegative count.").Max(100);
var date = V.Date().Nullish();
var name = V.String().MinLength(2).MaxLength(64);
```

## Collections

```csharp
var batch = V.Array(V.Item("Part").ID()).MinItems(1).MaxItems(50);
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

## DLL version

```csharp
var version = global::InnovatorSchema.SchemaInfo.GetVersion();
```

`GetVersion()` reads the version of the running DLL, including build metadata when present.
