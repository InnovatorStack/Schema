# Releases

[Downloads](https://github.com/InnovatorStack/Schema/releases)

[Schema 0.4.0](https://github.com/InnovatorStack/Schema/releases/tag/v0.4.0) adds
`ParseInput` for single Items and batches.

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
