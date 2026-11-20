# Property

Add a field to a new object or Item shape.

## Signature

```csharp
ObjectSchema ObjectSchema.Property<T>(string name, Schema<T> schema)
ObjectSchema ObjectSchema.Property(Field field)
ItemSchema ItemSchema.Property<T>(string name, Schema<T> schema)
ItemSchema ItemSchema.Property(Field field)
```

## Example

```csharp
var basePart = V.Item("Part");
var numberedPart = basePart.Property("item_number", V.String().NotBlank());
```

## Behavior

The original shape is unchanged. Duplicate binding identities, null fields and excessive field counts are rejected. Use the Field overload for attributes or relationship bindings. Property belongs to the structural shape; add properties before pipeline checks/transforms return a general `Schema<T>`.
