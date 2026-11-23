# Required and nullish fields

Declared fields require a nonblank value by default. `.Nullish()` allows omission,
explicit null, empty text and whitespace.

| AML property | String | String with Nullish |
| --- | --- | --- |
| Absent | Error | Accepted |
| `<name is_null="1" />` | Error | Accepted |
| `<name />` | Error | Accepted |
| `<name> </name>` | Error | Accepted |
| `<name>Bracket</name>` | Accepted | Accepted |

Other rules still apply to supplied nonblank values. For example, `V.Integer().Nullish()`
accepts an omitted property and rejects text such as `ten`.
