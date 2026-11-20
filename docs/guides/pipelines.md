# Pipelines

A pipeline reads input, checks values and applies transformations in declaration order.
Each operation returns a new immutable schema.

## Normalize before checking

```csharp
var code = V.String()
    .Transform(value => value.Trim())
    .NotBlank("Enter an item number.")
    .MaxLength(64)
    .Matches("^P-", "Start with P-.");
```

The string reader preserves the original text. The explicit transform trims it, and the
rules check that transformed value. Source input is not modified.

## Change the output type

```csharp
var length = V.String()
    .Transform(value => value.Length)
    .Min(1);
```

This schema produces an `int`. Its following rules check integers, not the original string.
Reusable declarations can hold the same transformed definition.

## Nullable output

`Transform` rejects an unexpected null return with `NullNotAllowed`. Use `TransformNullable`
when null is intentional. Reference and nullable-value transforms have separate overloads.

```csharp
var code = V.String().TransformNullable<string>(value =>
    value.Length == 0 ? null : value);
```

## Failures and callbacks

Transforms run after their source subtree succeeds. In all-issues mode, independent checks
on the same present value can continue after a check fails. A predicate must not assume an
earlier predicate passed. Put a dependent computation in a subsequent transform stage.

Caller exceptions propagate. The library only converts failures owned by its parsing or
built-in rule operations into validation issues. Cooperative cancellation also propagates.
