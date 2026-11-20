# Nested data

Compose schemas to describe supplied objects, expanded references and collections. Validation
does not fetch missing references or relationship rows from the database.

## Nested JSON objects

```csharp
var address = V.Object()
    .Property("city", V.String().NotBlank())
    .Property("postal_code", V.String().NotBlank());

var schema = V.Object()
    .Property("name", V.String().NotBlank())
    .Property("address", address);
```

A failure in the city field has path `$.address.city`.

## Expanded Item references

```csharp
var part = V.Item("Part")
    .Property("item_number", V.String().NotBlank());

var line = V.Item("Part BOM")
    .Property("quantity", V.DecimalText().Min(0.001m))
    .Property("related_id", part);
```

The `related_id` property must contain an expanded Part Item. An ID-only value produces an
`ExpansionRequired` issue. A nested Item of another type produces `ItemType`.

## Relationships

```csharp
var bom = V.Relationships("Part BOM", V.Array(line).MinCount(1));
var schema = part.Property(bom);
```

`Relationships` binds an explicit collection schema. It selects supplied rows whose type is
exactly `Part BOM`; rows of other types are not included. No matching rows produces an empty
collection, so `MinCount(1)` makes the relationship required.

```xml
<Item type="Part">
  <item_number>P-100</item_number>
  <Relationships>
    <Item type="Part BOM">
      <quantity>2</quantity>
      <related_id>
        <Item type="Part">
          <item_number>P-200</item_number>
        </Item>
      </related_id>
    </Item>
  </Relationships>
</Item>
```

A quantity failure has path `$.Relationships["Part BOM"][0].quantity`.

## Generic XML

Use an object schema for literal child names and `V.Attribute` for attributes. Generic XML
keeps `Item` as an ordinary element name and `is_null` as an ordinary attribute. Aras null
and expansion semantics apply through AML and IOM entry points.

An array schema reads the child elements of its selected container. It does not implicitly
group duplicate object-property elements into an array.
