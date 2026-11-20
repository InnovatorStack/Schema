# Introduction

Schema validates the input supplied to an Aras Server Method. Define the properties
and structure you expect, run validation, and return useful field errors when input is invalid.
The same library also validates JSON, XML and AML in ordinary C# applications.

## One definition, several checks

```csharp
var schema = V.Item("Part")
    .Property("item_number", V.String().NotBlank().MaxLength(64))
    .Property("weight", V.DecimalText().Min(0m).Optional());
```

This definition requires a `Part` Item with an `item_number`. A supplied `weight` must be
invariant numeric text representing a nonnegative decimal. Omission is permitted; explicit
null is a separate decision.

## The four parts

| Part | Purpose | Example |
| --- | --- | --- |
| Schema | Read a value or structure | `V.String()`, `V.Item("Part")` |
| Rule | Check an already-read value | `.NotBlank()`, `.Min(0m)` |
| Pipeline | Transform values or handle presence | `.Transform(...)`, `.Optional()` |
| Parsing | Return data or report validation issues | `V.Parse(schema, item)` |

Definitions are immutable. Adding a property, rule or transform returns a new definition.
They retain no Innovator session or request state, so a reusable definition can be shared.
Custom callbacks must also be safe for concurrent use.

## Validation and Aras errors

`Parse` returns data or throws `ValidationException`. `SafeParse` returns `Success`, `Data`
and `Error`. To return an Aras error Item, call `inn.newError(error.GetErrorString())` in your
Method. Schema retains no Innovator context.

## Use the output

`Parse` creates runtime objects from the schema. `SafeParse` returns output or a structured
error. Neither requires an output class. See [Parsed output](/guides/output).
