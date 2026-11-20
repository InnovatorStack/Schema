# String

Read a string without changing its contents.

## Signature

```csharp
Schema<string> V.String()
```

## Example

```csharp
var code = V.String().NotBlank().MaxLength(64);
```

## Behavior

JSON input must be a string. XML input must have scalar text rather than nested elements. Empty and whitespace-only text remain valid until a rule rejects them. Length rules count UTF-16 code units. Use an explicit transform to trim text.

## Related

[NotBlank](/api/not-blank) · [Transform](/api/transform)
