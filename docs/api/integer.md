# Integer

Read a native JSON number exactly as `long`.

## Signature

```csharp
Schema<long> V.Integer()
```

## Example

```csharp
var quantity = V.Integer().Min(0L);
```

## Behavior

The source representation is explicit: a JSON string is not a native number. Text readers also accept XML scalar property text. Conversion is culture-independent and rejects overflow, rounding and invalid syntax. Integer readers reject fractional values. Exponents are accepted when the value is exactly representable. Text readers do not trim whitespace. Invalid representation produces TypeMismatch; invalid numeric text or range produces InvalidNumber.

## Related

[Min](/api/min) · [Max](/api/max) · [Define a schema](/guides/schemas)
