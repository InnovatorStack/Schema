# Missing and null

Omitted input and explicit null are distinct states. Define both deliberately, especially
when omitted fields must be distinguished from an instruction to clear a value.

| Definition | Accepted states |
| --- | --- |
| `V.String()` | Present string |
| `V.String().Optional()` | Missing or present string |
| `V.String().Nullable()` | Null or present string |
| `V.String().Optional().Nullable()` | Missing, null or present string |

## JSON

For a property named `note`, `{}` means missing, `{"note":null}` means null, and
`{"note":""}` means a present empty string. Add `NotBlank` to reject an empty or whitespace-only string.

## Aras input

An absent property is missing. A selected property with `is_null="1"` is null. Aras null
markers must be exactly `0` or `1`; an empty marker is invalid, and a null property cannot
contain content.

```xml
<note is_null="1" />
```

These semantics belong to AML and IOM input. Ordinary XML does not reinterpret the attribute.

## Pipeline behavior

`Optional` and `Nullable` configure input presence, wherever they appear in a pipeline.
Present-value checks and transforms skip accepted missing/null states.

`RequireValue()` is a pipeline stage. It rejects missing or null output at that point,
including null deliberately introduced by an earlier nullable transform.

## Read states explicitly

`ValidationValue<T>` exposes `State`, `IsMissing`, `IsNull` and `HasValue`. Its `Value` getter
requires a present value. A valid nullable root can have no present output, so check
`HasOutput` or `OutputValue` before reading `result.Output`.

Optional data does not become an empty string, zero or false. Parsed output preserves the
difference between omission and explicit null through dictionary membership.
