- `OneOf(...)` replaces `Enum(...)` and works on all scalars. Use `.Insensitive()`
  after a string's `OneOf(...)` to ignore case.
- `V.Bool()` parses AML `0` and `1` as `false` and `true`.
- `ParseInput` returns `(input, inn)` for Items and batches. For AML text, `inn` is null.
- `.SqlQuoted()` turns a validated string into a SQL literal using Aras `QuoteValue`.

[Installation](https://innovatorstack.github.io/Schema/guides/installation.html) ·
[API](https://innovatorstack.github.io/Schema/api/)
