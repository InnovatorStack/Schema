- `Optional()` preserves empty strings and accepts omitted or null values.
- `Default(value)` supplies a validated fallback for omitted or null values.
- Use `MinLength`, `MaxLength`, and `Length` for strings. Use `String().Enum(...)`
  to limit a string to a set of values.
- `ID()` validates the Aras ID and returns its original string when parsing.
- Validation messages show the field and keep batch indexes in the error details.

[Installation](https://innovatorstack.github.io/Schema/guides/installation.html) ·
[API](https://innovatorstack.github.io/Schema/api/)
