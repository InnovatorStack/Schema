# InputFormat

Choose the representation of a text input.

## Signature

```csharp
InputFormat.Json
InputFormat.Xml
InputFormat.Aml
```

## Example

```csharp
var value = V.Parse(V.DecimalText(), "<amount>2.5</amount>", InputFormat.Xml);
```

## Behavior

Json is the default for generic text calls. Xml preserves ordinary elements and attributes. Aml applies Aras Item expansion and null-marker semantics and supports array roots. The format is explicit; payload contents are not guessed.
