# Optional fields and defaults

Declared fields require a nonblank value by default. `.Optional()` accepts a missing
field or an explicit null. Optional strings also accept an empty string.

| AML property | `V.String()` | `V.String().Optional()` |
| --- | --- | --- |
| Omitted | Error | `null` |
| `<name is_null="1"/>` | Error | `null` |
| `<name/>` | Error | `""` |
| `<name> </name>` | Error | Error |
| `<name>Bracket</name>` | `"Bracket"` | `"Bracket"` |

Length and choice rules also check `""`. For example, `.Optional().MinLength(1)`
accepts omission and rejects `<name/>`. An optional integer, double or date
accepts null and still requires a valid value when supplied.

Use `.Default(value)` to supply a value when the field is omitted or explicitly null.

```csharp
var schema = V.Item().Property("type", V.String().OneOf("user", "identity").Default("user"));
var (item, _) = V.ParseInput(schema, "<Item/>");
string type = item.type; // "user"
```

`<type/>` and `<type>admin</type>` fail validation. Schema checks the default against
the builder's rules while you define it. Defaults appear in the parsed values, and
the original AML or IOM Item stays as received.
