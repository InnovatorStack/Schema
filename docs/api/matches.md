# Matches

Match a culture-invariant regular expression with a bounded deadline.

## Signature

```csharp
Rule<string> V.Matches(string pattern, string? message = null)
Schema<string> schema.Matches(string pattern, string? message = null)
```

## Example

```csharp
var schema = V.String().Matches("^P-", "Start with P-.");
```

## Behavior

A failed check produces `Pattern` unless execution reaches a resource limit. The message argument overrides the rule's default text. These checks do not trim or mutate values. The fluent overload checks the current string output; the factory creates a reusable `Rule<string>`. Patterns are bounded to 4,096 characters. Invalid regex syntax throws at construction. A built-in regex timeout produces LimitExceeded; the deadline is 25 ms.
