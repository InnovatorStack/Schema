# UnknownProperties

Choose whether a new shape accepts undeclared properties.

## Signature

```csharp
ObjectSchema ObjectSchema.UnknownProperties(UnknownPropertyHandling handling)
ItemSchema ItemSchema.UnknownProperties(UnknownPropertyHandling handling)
```

## Example

```csharp
var schema = V.Object()
    .Property("name", V.String())
    .UnknownProperties(UnknownPropertyHandling.Allow);
```

## Behavior

`Reject` reports UnknownProperty for undeclared source members. `Allow` permits them but does not validate, convert or add them to declared output. Object defaults to Reject; Item defaults to Allow. Duplicate properties are still errors. Unsupported enum values throw.
