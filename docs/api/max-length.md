# MaxLength

Limit string length in UTF-16 code units.

## Signature

```csharp
Rule<string> V.MaxLength(int length, string? message = null)
Schema<string> schema.MaxLength(int length, string? message = null)
```

## Example

```csharp
var schema = V.String().MaxLength(64);
```

## Behavior

A failed check produces `MaximumLength` unless execution reaches a resource limit. The message argument overrides the rule's default text. These checks do not trim or mutate values. The fluent overload checks the current string output; the factory creates a reusable `Rule<string>`. Negative lengths and incompatible bounds in a contiguous rule segment are construction errors.
