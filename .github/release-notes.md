- Check Aras IDs with `.ID(V.Guid())` or validate an ID stored in a property.
- `AssertInput` throws messages like “Weight must be a whole number.” Use
  `GetErrorString()` for the AML paths and validation details.
- Call `SchemaInfo.GetVersion()` to see which DLL the Method loaded.

[Installation](https://innovatorstack.github.io/Schema/guides/installation.html) ·
[API](https://innovatorstack.github.io/Schema/api/)
