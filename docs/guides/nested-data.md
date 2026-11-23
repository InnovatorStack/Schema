# Nested Items

A property can hold its own Item schema. The property's name comes from the declaration.

```csharp
var part = V.Item("Part").ID();
var schema = V.Item("Part BOM")
    .Property("related_id", part)
    .Property("quantity", V.Integer().Min(1));
```

```xml
<Item type="Part BOM">
    <related_id>
        <Item type="Part" id="783BA98DA5D04BF9AF4B1760826221F0" />
    </related_id>
    <quantity>2</quantity>
</Item>
```

Supply an expanded Item for a nested schema. A property containing only an ID is text.
