# BooleanText

Read exact boolean text according to a selected token format.

## Signature

```csharp
Schema<bool> V.BooleanText(
    BooleanTextFormat format = BooleanTextFormat.ZeroOne)
```

## Example

```csharp
var flag = V.BooleanText(BooleanTextFormat.Either);
```

## Behavior

`ZeroOne` accepts `0` and `1`. `TrueFalse` accepts `false` and `true`. `Either` accepts all four. Matching is case-sensitive and does not trim. Unsupported format enum values throw at definition construction. Wrong source kinds produce TypeMismatch; other text produces InvalidBoolean.
