# Validate a Part

This Method expects the current input Item to have type `Part`. It requires an item number and
checks a supplied weight. An omitted weight is accepted; a negative or malformed value is not.

<<< ../../examples/PartMethod.cs

The example passes the current Item explicitly; see [Server Methods](/guides/server-methods).
On success it
returns the original Item. It does not normalize or save the source.

## Valid input

```xml
<Item type="Part" action="ValidatePart">
  <item_number>P-100</item_number>
  <weight>1.5</weight>
</Item>
```

## Invalid input

```xml
<Item type="Part" action="ValidatePart">
  <item_number> </item_number>
</Item>
```

The blank value produces a field error at `$.item_number` with the configured message.
A missing item number instead produces `Required`.
