- Use `V.Items(itemSchema)` for Item collections.
- `V.Array(element, delimiter)` parses a delimited property or attribute into a typed array.
- `V.Guid()` returns the original validated Aras ID string.
- `.SqlQuoted()` escapes apostrophes and adds plain single quotes. `.SqlQuoted(false)`
  escapes apostrophes alone. Both work with `ParseInput(schema, input)`.

[Installation](https://innovatorstack.github.io/Schema/guides/installation.html) ·
[API](https://innovatorstack.github.io/Schema/api/)
