# Max

Check an inclusive upper bound.

## Signature

```csharp
Rule<T> V.Max<T>(T value, string? message = null)
Schema<T> schema.Max(T value, string? message = null)
// T : struct, IComparable<T>
```

## Example

```csharp
var schema = V.DecimalText().Max(0m);
```

## Behavior

Compares the current CLR value to the supplied bound. A failure produces Maximum. Use the matching CLR type, such as 1L for an Int64 bound. The factory creates a reusable `Rule<T>`. Incompatible bounds within a contiguous rule segment are rejected during construction; transforms separate segments.
