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

Restart the Aras application. Invoke a Method using Schema to confirm the DLL loads.

## Install in a subfolder

Reference the DLL's actual path in `method-config.xml`. For example:

```xml
<name>$(binpath)/X_Utils/InnovatorSchema/InnovatorSchema.dll</name>
```

For a framework-dependent Aras server, add the absolute directory to `APP_PATHS` under
`runtimeOptions.configProperties` in `Server/bin/Aras.Server.runtimeconfig.json`.
Keep the other runtime settings and restart the application. Check Syntax uses the
reference in `method-config.xml`. Invocation uses the runtime assembly search path.
