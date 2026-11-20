# NotBlank

Require at least one non-whitespace character.

## Signature

```csharp
Rule<string> V.NotBlank(string? message = null)
Schema<string> schema.NotBlank(string? message = null)
```

## Example

```csharp
var schema = V.String().NotBlank("Enter a code.");
```

## Behavior

A failed check produces `NotBlank` unless execution reaches a resource limit. The message argument overrides the rule's default text. These checks do not trim or mutate values. The fluent overload checks the current string output; the factory creates a reusable `Rule<string>`.
