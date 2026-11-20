# WithMessage

Override a diagnostic message within one schema boundary.

## Signature

```csharp
Schema<T> schema.WithMessage(IssueCode code, string message)
```

## Example

```csharp
var code = V.String()
    .WithMessage(IssueCode.Required, "Enter an item number.")
    .NotBlank("Enter an item number.");
```

## Behavior

Returns an immutable schema with a scoped code-to-message override. Child field definitions establish their own message scope; the parent override does not leak into them. Invalid issue enum values and empty/oversized messages throw during definition construction. Rule-specific message arguments remain useful for direct rule configuration.
