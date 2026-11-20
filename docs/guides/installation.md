# Installation

Choose the .NET 6 or .NET 8 build for your server. Each contains one library DLL and uses
framework assemblies only.

## Add the library to Aras

1. Copy `InnovatorSchema.dll` into your instance's `Server/bin` directory.
2. Open `Server/method-config.xml` and add this entry inside `ReferencedAssemblies`:

```xml
<name>$(binpath)/InnovatorSchema.dll</name>
```

3. Add these declarations to the CSharp Method template:

```csharp
using InnovatorSchema;
using V = InnovatorSchema.Validator;
```

4. Apply the changes through your normal deployment and restart procedure.

Pass the input explicitly: `V.Parse(schema, this)` for an Item or `V.Parse(schema, json)`
for JSON text. See [Server Methods](/guides/server-methods).

## Use it outside Aras

Reference the same `InnovatorSchema.dll` from your application. JSON, XML and AML parsing
work without IOM installed.

```csharp
using InnovatorSchema;
using V = InnovatorSchema.Validator;

var name = V.Parse(V.String().NotBlank(), "\"Bracket\"");
```
