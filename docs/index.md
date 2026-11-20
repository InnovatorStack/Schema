# Schema

Define a C# schema for the data supplied to an Aras Server Method. Validate properties,
expanded Items and relationships, then handle precise field errors.

```csharp
var schema = V.Item("Part")
    .Property("item_number", V.String()
        .NotBlank("Enter an item number.")
        .MaxLength(64))
    .Property("weight", V.DecimalText().Min(0m).Optional());

var part = V.Parse(schema, this);
return this.getInnovator().newResult(part.item_number);
```

Pass the current Item explicitly. [Server Method setup](/guides/server-methods) covers the
assembly reference and imports. The schema
checks the supplied Item without changing it or fetching references.

## Start with your input

| Input | Entry point | Guide |
| --- | --- | --- |
| Current Aras Item | `V.Parse(schema, item)` | [Server Methods](/guides/server-methods) |
| AML text | `V.ValidateAml(schema, aml)` | [Nested data](/guides/nested-data) |
| JSON text | `V.ValidateJson(schema, json)` | [Define a schema](/guides/schemas) |
| Ordinary XML | `V.ValidateXml(schema, xml)` | [ValidateXml](/api/validate-xml) |

## Read validated data

Schemas read values. Rules check them. Pipelines transform successful values. Results retain
issues with codes, messages and paths such as `$.item_number` and
`$.Relationships["Part BOM"][0].quantity`.

Read [Missing and null](/guides/presence) before accepting optional data, and
[Handle errors](/guides/errors) to choose between plain results and Aras error Items.

## Install the matching build

Each framework build contains one `InnovatorSchema.dll` with framework dependencies only.
[Installation](/guides/installation) covers the assembly reference and namespace imports.

[Parsed output](/guides/output) covers runtime fields, collections and serialization.
