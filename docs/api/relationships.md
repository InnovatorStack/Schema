# Relationships

Bind an explicit collection schema to supplied relationships of an exact Item type.

## Signature

```csharp
Field<T> V.Relationships<T>(string itemType, Schema<T> collection)
```

## Example

```csharp
var line = V.Item("Part BOM")
    .Property("quantity", V.DecimalText().Min(0.001m));
var bom = V.Relationships("Part BOM", V.Array(line).MinCount(1));
var schema = V.Item("Part", bom);
```

## Behavior

The schema must read a collection; scalar/object readers are rejected during construction. Checked or transformed collection schemas retain their collection input capability. Selection preserves source order and ignores other relationship types. No matching rows means an empty collection. Use MinCount to require rows. The binder never adds another array layer or fetches relationships.

## Related

[Array](/api/array) · [Nested data](/guides/nested-data)
