# Define a schema

A schema reads a value and applies its checks. Factories live on `InnovatorSchema.Validator`,
usually imported with `using V = InnovatorSchema.Validator;`.

## Scalar values

```csharp
var code = V.String().NotBlank().MaxLength(64);
var quantity = V.Decimal().Min(0.001m);
var count = V.Integer().Min(1L);
var enabled = V.Boolean();
```

`Decimal`, `Integer` and `Boolean` require native JSON representations. Their text variants
read strings such as XML property values:

```csharp
var weight = V.DecimalText().Min(0m);
var count = V.IntegerText().Min(1L);
var enabled = V.BooleanText(BooleanTextFormat.ZeroOne);
```

Text conversion is exact and culture-independent. It does not trim whitespace. Numeric
overflow, fractional integers and decimal rounding are validation failures.

## Objects

```csharp
var schema = V.Object()
    .Property("item_number", V.String().NotBlank())
    .Property("quantity", V.Decimal().Min(0.001m));

var result = V.ValidateJson(schema,
    "{\"item_number\":\"P-100\",\"quantity\":2.5}");
```

Objects accept undeclared properties by default. Use
`.UnknownProperties(UnknownPropertyHandling.Reject)` for a strict shape. Extra properties
are excluded from parsed output.

## Items

```csharp
var schema = V.Item("Part")
    .Property("item_number", V.String().NotBlank())
    .Property("weight", V.DecimalText().Min(0m).Optional());
```

Item schemas require an exact unqualified Item type. They allow undeclared properties by
default, accommodating ordinary Aras metadata. Declared nested Items require supplied expansion.

## Arrays

```csharp
var schema = V.Array(V.String().NotBlank()).MinCount(1).MaxCount(100);
```

Every entry uses the same element schema. Input order and accepted null positions are retained.

## Finish the shape before the pipeline

Add properties and choose unknown-property handling before whole-object checks or transforms.
Pipeline operations return a general `Schema<T>` because the output may change type.
