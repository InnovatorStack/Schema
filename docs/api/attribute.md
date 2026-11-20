# Attribute

Bind a schema to an XML attribute.

## Signature

```csharp
Field<T> V.Attribute<T>(string name, Schema<T> schema)
```

## Example

```csharp
var action = V.Attribute("action", V.String());
var schema = V.Item("Part", action);
```

## Behavior

XML attribute values are scalar text. Use text numeric/boolean readers when converting them. The attribute path is rendered as `$[@"action"]`. Missing attributes follow the bound schema's presence policy. This binding is intended for XML/AML/IOM input.

## Related

[Nested data](/guides/nested-data)
