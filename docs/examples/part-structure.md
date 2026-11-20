# Validate a Part structure

Reuse one Part shape for the root and expanded references. Each BOM row must have a positive
quantity and a supplied related Part. The root requires at least one matching relationship.

<<< ../../examples/AmlMethod.cs

The Method reads AML from `payload`. A plain facade call can also validate AML without IOM:

```csharp
var result = V.ValidateAml(schema, aml);
```

## AML payload

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

An ID-only related value fails with `ExpansionRequired`. No matching BOM rows fails with
`MinimumCount`. Validation uses only the supplied data and does not fetch either kind of input.
