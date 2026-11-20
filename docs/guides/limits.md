# Limits and cancellation

Validation is bounded by per-call options and definition-construction limits. These protect
the host from unbounded traversal, issue collection and rule execution.

## Per-call options

```csharp
var options = new ValidationOptions(
    mode: ValidationMode.FirstIssue,
    maximumInputLength: 262144,
    maximumCollectionLength: 1000,
    maximumIssues: 20);

var result = V.ValidateAml(schema, aml, options);
```

| Option | Default | Maximum |
| --- | ---: | ---: |
| `MaximumInputLength` | 1,048,576 | 16,777,216 |
| `MaximumDepth` | 64 | 128 |
| `MaximumNodes` | 100,000 | 1,000,000 |
| `MaximumCollectionLength` | 10,000 | 100,000 |
| `MaximumIssues` | 100 | 1,000 |
| `MaximumOperations` | 100,000 | 10,000,000 |

Limits must be positive. Invalid option values throw during construction. Text length counts
UTF-16 code units. Parsing and input inspection include data outside the selected schema.

## First issue or all issues

`AllIssues` collects independent failures until a limit is reached. `FirstIssue` stops after
the first failure. When issue collection overflows, the final retained issue becomes a
`LimitExceeded` marker rather than allowing the list to grow without bound.

## Cancellation

Pass a `CancellationToken` through `ValidationOptions`. Cancellation is checked between
bounded operations and propagates as `OperationCanceledException`; it is not a validation issue.
Caller predicates and transforms are not forcibly interrupted.

## Definition limits

Definitions allow up to 256 fields per object, 64 structural levels, 64 pipeline stages,
16,384 expanded graph nodes and 4,096 rules per object graph. A contiguous rule segment permits
up to 64 checks. Invalid definitions throw before input validation.

Names are bounded to 256 characters, patterns to 4,096, rule ids to 128 and configured messages
to 1,024. Input paths and property names are separately bounded during execution.

## Regular expressions

Built-in `Matches` uses a 25 ms deadline. A timeout produces `LimitExceeded`. Scheduling
pressure can contribute to a wall-clock deadline. A caller-thrown regex exception still
propagates normally.
