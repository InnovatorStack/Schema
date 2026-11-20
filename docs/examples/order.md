# Validate an order

The current Item carries JSON in its `payload` property. The schema requires a nonblank item
number and a native decimal quantity of at least 0.001.

<<< ../../examples/JsonMethod.cs

## Payload

```json
{
  "item_number": "P-100",
  "quantity": 2.5
}
```

`"quantity": "2.5"` is a type failure here. Use `DecimalText` only when the intended input is
a string. This distinction keeps accidental coercions visible.

An undeclared JSON property produces `UnknownProperty` because object schemas are strict
by default. The Method returns its original context Item on success.
