# Check

Attach reusable or custom checks to the current output value.

## Signature

```csharp
Rule<T> V.Check<T>(Func<T, bool> predicate, string ruleId, string message)
Schema<T> schema.Check(Rule<T> rule)
Schema<T> schema.Check(Func<T, bool> predicate, string ruleId, string message)
```

## Example

```csharp
var schema = V.String().Check(
    value => value.StartsWith("P-", StringComparison.Ordinal),
    "part.prefix",
    "Start with P-.");
```

## Behavior

Custom predicates produce Custom issues with the supplied RuleId and message. They run only for present values and must be safe for concurrent use. Caller exceptions propagate. All-issues mode can continue independent checks after a failure, so predicates must not assume an earlier check passed. Whole-object checks report at the object path.
