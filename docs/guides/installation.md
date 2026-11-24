# Installation

1. Download the build for your Aras server's .NET version from [Releases](/releases/).
2. Copy `InnovatorSchema.dll` directly into that instance's `Server/bin`.
3. Add this entry inside `ReferencedAssemblies` in `Server/method-config.xml`.

```xml
<name>$(binpath)/InnovatorSchema.dll</name>
```

Add these imports to the C# Method template in the same file.

```csharp
using InnovatorSchema;
using V = InnovatorSchema.Validator;
```

Restart the Aras application after installation.

## Check the loaded version

Invoke a Method that returns the version of the DLL Aras loaded.

```csharp
return this.getInnovator().newResult(global::InnovatorSchema.SchemaInfo.GetVersion());
```

## Install in a subfolder

Include the subfolder in the DLL path under `ReferencedAssemblies` in `method-config.xml`.

For a framework-dependent Aras server, add the absolute directory to `APP_PATHS` under
`runtimeOptions.configProperties` in `Server/bin/Aras.Server.runtimeconfig.json`.
Keep the other runtime settings and restart the application. Check Syntax uses the
reference in `method-config.xml`. Invocation uses the runtime assembly search path.
