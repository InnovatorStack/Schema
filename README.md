<h1 align="center">Schema</h1>

<p align="center">Validation and parsing for Aras Innovator.</p>

<p align="center">
  <a href="https://innovatorstack.github.io/Schema/">Documentation</a> ·
  <a href="https://innovatorstack.github.io/Schema/api/">API reference</a> ·
  <a href="https://innovatorstack.github.io/Schema/examples/part.html">Examples</a>
</p>

Define what your Method needs. Parse the input and use the data directly.

```csharp
var partSchema = V.Item("Part")
    .Property("item_number", V.String()
        .NotBlank("Enter an item number.")
        .MaxLength(64, "Use at most 64 characters."))
    .Property("weight", V.DecimalText()
        .Min(0m, "Weight cannot be negative.")
        .Optional());

var part = V.Parse(partSchema, this);
return this.getInnovator().newResult(part.item_number);
```

`part.item_number` is a string. `part.weight` is a decimal when supplied. Nested schemas
produce nested objects and lists, without declaring output classes.

- **Validate what you need.** Extra fields are accepted and excluded from parsed data.
- **Keep the whole structure.** Compose schemas for Items, relationships, objects and arrays.
- **Get every issue.** Validation collects independent failures with field paths and messages.

## Use in Aras

1. Choose a [release build](https://github.com/InnovatorStack/Schema/releases) for your server.
2. Copy `InnovatorSchema.dll` into `Server/bin` and reference it in `Server/method-config.xml`.
3. Add these imports to the CSharp Method template:

```csharp
using InnovatorSchema;
using V = InnovatorSchema.Validator;
```

Then use the example above in your Method. Pass the Item explicitly as `V.Parse(schema, this)`.
The [installation guide](https://innovatorstack.github.io/Schema/guides/installation.html)
covers the assembly reference and server setup.

## Parse a collection

Wrap the Item schema in `V.Array` and pass an Item selection:

```csharp
var items = this.getItemsByXPath("parts/Item");
var parts = V.Parse(V.Array(partSchema), items);

string firstNumber = parts[0].item_number;
```

Use the same composition for
[nested Items and relationships](https://innovatorstack.github.io/Schema/guides/nested-data.html).
The [assembly example](https://innovatorstack.github.io/Schema/examples/assembly.html) combines
BOM rows, expanded components, document relationships and a weight calculation.

## Handle errors

`Parse` returns data or throws `ValidationException` containing the validation issues.
Use `SafeParse` when you want to handle a failed validation without an exception:

```csharp
var result = V.SafeParse(partSchema, this);

if (!result.Success)
{
    return this.getInnovator().newError(result.Error.GetErrorString());
}

var part = result.Data;
return this.getInnovator().newResult(part.item_number);
```

For your own error handling, `result.Error.GetIssues()` returns the structured issues and
`result.Error.GetErrorString()` returns their summary.

## JSON, XML and AML

The same API accepts text, including in applications without IOM:

```csharp
var schema = V.Object()
    .Property("name", V.String().NotBlank())
    .Property("quantity", V.Integer().Min(1L));

var order = V.Parse(schema, "{\"name\":\"Bracket\",\"quantity\":4}");
```

JSON is the default. Pass `InputFormat.Xml` or `InputFormat.Aml` for XML or AML text.
Each build contains one library DLL with framework dependencies only.

---

[MIT license](LICENSE) · [Contributing](https://innovatorstack.github.io/Schema/contributing.html)
