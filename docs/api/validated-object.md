# ValidatedObject

Read validated object fields through their declaration handles.

## Signature

```csharp
T value.Get<T>(Field<T> field)
ValidationValue<T> value.Read<T>(Field<T> field)
```

## Example

```csharp
var number = V.Field("item_number", V.String());
var result = V.ValidateJson(V.Object(number), "{\"item_number\":\"P-100\"}");
string text = result.Output.Get(number);
```

## Behavior

Get requires a present value; Read preserves presence. A handle from a different declaration is rejected even if its name matches. This view is read-only and does not retain a mutable Innovator context. Direct serialization is not a field-output contract. Use Parse or SafeParse for named runtime properties and JSON serialization.

## Related

[Validated output](/guides/output)
