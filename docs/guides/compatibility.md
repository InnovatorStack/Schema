# Compatibility

Schema has no IOM assembly reference or production NuGet dependencies. The Item reader
uses the supplied Item's public XML members; it does not obtain an Innovator instance.

## Framework targets

| Build | Minimum host runtime |
| --- | --- |
| `net6.0` | .NET 6 |
| `net8.0` | .NET 8 |

The .NET 6 DLL also runs on newer .NET hosts. Framework compatibility still applies:
these builds cannot be loaded into a .NET Framework application.

## Item input

`V.Parse(schema, item)` and the corresponding `SafeParse` and `Validate` calls read `node`,
`nodeList` and `dom`. Accessors are cached once per loaded Item type. Selected collections,
expanded Items and relationships use the supplied DOM.

The same compiled library is checked against IOM 14.0.22.40048 and IOM 15. These are test
inputs, not library references or separate release profiles. Additional Aras versions must
preserve the public Item XML contract.

## Method syntax

Consumer Method examples use a C# 6 syntax baseline. The library itself is built with the
SDK pinned in `global.json`; library source syntax and server Method syntax are independent.

## Deployment checks

Choose the framework build for the target process. After deployment, invoke a Method with
valid and invalid input to confirm the assembly reference and Method imports are configured.
