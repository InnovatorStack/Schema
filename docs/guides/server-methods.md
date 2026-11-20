# Server Methods

Reference `InnovatorSchema.dll` in `Server/method-config.xml` and add these imports to the
CSharp template:

```csharp
using InnovatorSchema;
using V = InnovatorSchema.Validator;
```

Pass the Item or payload explicitly.

## Parse a Part

<<< ../../examples/ParsePartMethod.cs

`Parse` returns the object directly. Its properties and native values come from the schema.
Invalid input throws `ValidationException` containing all collected issues.

## Parse a list of Parts

<<< ../../examples/ParsePartsMethod.cs

`getItemsByXPath` selects supplied DOM nodes without querying the server. An array schema
parses the complete selection. A single Item schema requires exactly one Item.

## Parse text

```csharp
var order = V.Parse(orderSchema, this.getProperty("payload"));
var part = V.Parse(partSchema, aml, InputFormat.Aml);
```

The Item reader accesses the supplied XML only. It has no IOM assembly reference and does
not obtain an Innovator instance.

`SafeParse` exposes `Success`, `Data` and `Error`. Use it to handle validation failures
without catching an exception. See [Handle errors](/guides/errors) for plain errors and
explicit Aras error conversion.
