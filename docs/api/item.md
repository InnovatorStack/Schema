# Item

Require an Aras Item with an exact type and validate its declared properties.

## Signature

```csharp
ItemSchema V.Item(string itemType, params Field[] fields)
```

## Example

```csharp
var schema = V.Item("Part")
    .Property("item_number", V.String().NotBlank())
    .Property("weight", V.DecimalText().Min(0m).Optional());
```

## Behavior

Use AML or IOM entry points. The Item element must be unqualified and its type must match exactly. Unknown properties are allowed by default. A nested property must contain supplied expansion; an ID-only reference fails with ExpansionRequired. Item fields require unqualified XML names. This schema does not query or save anything.

## Related

[Validate](/api/validate) · [ValidateAml](/api/validate-aml)
