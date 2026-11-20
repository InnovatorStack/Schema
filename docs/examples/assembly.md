# Parse an assembly

Validate BOM quantities, expanded components and their document relationships. The Method
calculates total component weight from the parsed decimal values.

<<< ../../examples/AssemblyMethod.cs

The `code` schema is reused for assembly, component and document numbers. Its transform
trims the parsed value while leaving the source Item unchanged. Missing or null component
weights contribute zero to the total. Each component must include at least one expanded
document.

## Input

<<< ../../tests/Fixtures/Assembly.xml

This input returns `4.00`. Additional fields and unrelated relationship types are accepted.

## Nested errors

An invalid revision on the first component's first document produces an issue at:

```text
$.Relationships["Part BOM"][0].related_id.Relationships["Part Document"][0].related_id.major_rev
```

`Parse` throws `ValidationException` with all independent issues. Use
[SafeParse](/api/safe-parse) to inspect the same errors without catching an exception.
