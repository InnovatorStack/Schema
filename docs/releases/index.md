# Releases

[Downloads](https://github.com/InnovatorStack/Schema/releases)

## Upgrading to 0.7.0

- Use `V.Items(itemSchema)` for Item collections and `V.Array(element, delimiter)`
  for delimited fields.
- `V.Guid()` returns the original validated string.
- Call `ParseInput(schema, input)` for schemas that use `.SqlQuoted()`. Quoted values
  use plain single quotes. `.SqlQuoted(false)` returns the escaped text.

## Upgrading to 0.6.0

- Replace `V.Enum(...)` and `V.String().Enum(...)` with `V.String().OneOf(...)`.
- Unpack `ParseInput` with `var (item, inn) = V.ParseInput(schema, this)`.
  Use `var (item, _)` when you only need the parsed values.
- Use `V.Bool()` for AML booleans and `SqlQuoted()` for SQL string literals.

## Upgrading to 0.5.0

- Use `MinLength`, `MaxLength`, or `Length` for strings.
- Replace `Nullish()` with `Optional()` to preserve empty strings. Empty values
  on optional numbers, dates and GUIDs fail validation.
- `ID()` now requires a valid Aras ID. It still returns the original string.

| Asset | Minimum runtime |
| --- | --- |
| `Schema-<version>-net6.0.zip` | .NET 6 |
| `Schema-<version>-net8.0.zip` | .NET 8 |

## Manual publication

Releases are created by the manually triggered **Release** workflow. Draft mode is the default.
See [Release workflow](/releases/workflow).
