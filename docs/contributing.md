# Contributing

Run the C# and documentation checks before submitting a change.

```bash
dotnet restore --locked-mode
dotnet test --configuration Release --no-restore
dotnet format --verify-no-changes --no-restore
npm ci
npm run docs:check
npm run docs:build
```

Keep the library in one assembly, with framework dependencies only. Use braces and keep
source lines within 100 columns and functions within 70 lines.

The tests compile examples with C# 6 and run against IOM 15 by default. To test a different
IOM release, pass its `IOM.dll` path with `-p:TestIomPath=/path/to/IOM.dll` and choose a
separate `--artifacts-path`.
