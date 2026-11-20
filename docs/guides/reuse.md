# Reuse schemas

Reuse the same immutable definition wherever the same contract applies.

```csharp
var itemNumber = V.String().NotBlank().MaxLength(64);
var part = V.Item("Part").Property("item_number", itemNumber);

var line = V.Item("Part BOM")
    .Property("quantity", V.DecimalText().Min(0.001m))
    .Property("related_id", part);

var structure = part.Property(V.Relationships("Part BOM", V.Array(line).MinCount(1)));
```

`part` remains unchanged when `structure` adds a relationship binding.

## Named declarations

Use `Schema<T>` to give a reusable definition a name. Its protected constructor accepts the
same definition used inline, including checks and transforms.

```csharp
public sealed class CodeSchema : Schema<string>
{
    public static CodeSchema Instance { get; } = new CodeSchema();

    public CodeSchema() : base(V.String()
        .Transform(value => value.Trim())
        .NotBlank()
        .MaxLength(64))
    {
    }
}
```

A normal static readonly `Schema<T>` is also valid. Named declaration classes belong in
ordinary C# application code; a raw Aras Method body can simply reuse local definitions.

## Reuse rules across representations

```csharp
var positiveQuantity = V.Min(0.001m).And(V.Max(1000m));
var jsonQuantity = V.Decimal().Check(positiveQuantity);
var amlQuantity = V.DecimalText().Check(positiveQuantity);
```

A rule checks a value; it does not decide how to read it. Rules are reusable across JSON
numbers and Aras property text once those representations produce the same CLR type.
