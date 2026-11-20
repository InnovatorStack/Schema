# Array

Validate each supplied element with one schema, preserving source order.

## Signature

```csharp
Schema<ValidatedArray<T>> V.Array<T>(Schema<T> element)
```

## Example

```csharp
var codes = V.Array(V.String().NotBlank()).MinCount(1);
```

## Behavior

JSON input must be an array. XML input reads child elements of the selected container. Accepted null entries retain their indexes as `ValidationValue<T>`. Element issues use indexed paths. A null element schema throws at construction. Collection-length resource limits apply separately from MinCount/MaxCount rules.

## Related

[MinCount](/api/min-count) · [ValidatedArray](/api/validated-array)
