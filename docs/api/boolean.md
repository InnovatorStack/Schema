# Boolean

Read a native JSON boolean.

## Signature

```csharp
Schema<bool> V.Boolean()
```

## Example

```csharp
var enabled = V.Boolean().Optional();
```

## Behavior

Only native true/false values are accepted. Strings and numeric 0/1 are type failures. Use BooleanText for XML property text or explicitly string-encoded JSON booleans.

## Related

[BooleanText](/api/boolean-text)
