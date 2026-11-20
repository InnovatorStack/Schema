# Contributing

Schema is released under the MIT license.

## Build and check

Install the SDK pinned by `global.json` and .NET 6/.NET 8 test runtimes.

```sh
dotnet restore --locked-mode
dotnet build -c Release --no-restore
dotnet test -c Release --no-build
dotnet format --verify-no-changes --no-restore
```

The host harness compiles consumer examples and executes offline genuine-IOM fixtures whose
transport throws if called. Live test configuration and proprietary IOM binaries stay local.

## Documentation

```sh
npm ci
npm run docs:check
npm run docs:build
npm run docs:preview
```

Documentation describes the supported API and its contracts. New examples should compile
against that API.
The test suite compiles API example blocks with C# 6. VitePress 1.6 uses a pinned Vite 6
override because its original Vite 5 dependency has unpatched development-server advisories.
Check both the site build and local preview when updating that override.

## Engineering conventions

Keep the runtime in one assembly and public namespace. Preserve immutable definitions,
per-call execution state, explicit source representations and bounded work. New capabilities
must compose with presence, transforms, issue paths and limits.

Owned C# follows four-space indentation, braces, a 100-column limit and a 70-line physical
function limit. Nullable analysis, analyzers and warnings as errors stay enabled.

## Offline Method checks

The host harness executes the example bodies and 116 payload cases with genuine IOM Items.
Its transport throws if code attempts a server request. Run it after building:

```sh
dotnet tests/InnovatorSchema.HostTests/bin/Release/net8.0/InnovatorSchema.HostTests.dll
dotnet run --project examples/Prototype -c Release
```

A server `bin` argument also checks the installed CSharp template in memory. These checks
do not deploy Methods. The `TestIomPath` property selects a local IOM DLL for tests only. Use an isolated artifacts
directory when selecting another version so workstation restore state is not overwritten.

## Live verification

Live checks use Python 3's standard library and the same exported payload cases. Install the
matching DLL and compiler reference first, then export the cases from that build's host harness:

```sh
mkdir -p .local
dotnet tests/InnovatorSchema.HostTests/bin/Release/net8.0/InnovatorSchema.HostTests.dll \
    --export-server-cases .local/server-cases.json
python3 tests/live/run.py --url http://your-server/InnovatorServer/ \
    --database YourDatabase --username YourTestUser \
    --package .local/server-cases.json --work-dir .local/live/run-01
```

The runner prompts for the password; `--password-env VARIABLE_NAME` supports automation. It
creates three uniquely named Methods restricted to that user's execution identity, verifies
the loaded DLL's module ID, runs 116 cases and 34 concurrent isolation checks, and records
Method IDs and results in the work directory. Tokens remain in memory. Method registration
uses `ApplyAML`; invocation uses the `ApplyMethod` SOAP action with an XML context Item.

Use `--reuse-deployment .local/live/run-01/deployment.json` with a new work directory to rerun
existing Methods. The runner verifies their names and code hashes before invoking them. Methods
created by a successful run are removed after verification; `--keep-methods` retains them for
debugging. Failed runs and reused deployments are retained. Diagnostic Methods and consumer
examples are separate; the journal identifies exactly which items belong to each test run.
