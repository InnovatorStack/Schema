# MinLength

Require a minimum string length in UTF-16 code units.

## Signature

```csharp
Rule<string> V.MinLength(int length, string? message = null)
Schema<string> schema.MinLength(int length, string? message = null)
```

## Example

```csharp
var schema = V.String().MinLength(3);
```

## Behavior

A failed check produces `MinimumLength` unless execution reaches a resource limit. The message argument overrides the rule's default text. These checks do not trim or mutate values. The fluent overload checks the current string output; the factory creates a reusable `Rule<string>`. Negative lengths and incompatible bounds in a contiguous rule segment are construction errors.
