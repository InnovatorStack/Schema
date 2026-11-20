# ParsedObject

Read parsed fields through runtime properties or string indexes.

## Signature

```csharp
dynamic output
object? output[string key]
bool output.ContainsKey(string key)
bool output.TryGetValue(string key, out object? value)
```

## Example

```csharp
var schema = V.Object().Property("part-number", V.String());
var part = V.Parse(schema, "{\"part-number\":\"P-100\"}");
string number = part["part-number"];
```

## Behavior

Implements `IReadOnlyDictionary<string, object?>`. Only supplied declared fields are enumerated or serialized. Omitted optional fields read as null and have ContainsKey false. Unknown names fail normally. Attributes use @name keys; relationships are grouped under Relationships by type. Output is read-only. C# dynamic access provides runtime properties without caller type declarations.
