# Performance

Schema construction and validation are separate costs. Reuse definitions when they have a
suitable lifetime. Each call has its own execution state and owned output.

## Validation and parsing

`Validate` returns typed views. `SafeParse` and `Parse` additionally materialize named objects.
Item input reads the supplied DOM; text input includes parsing. Neither performs database I/O.

A local .NET 8 measurement with reused definitions gives:

| Workload | Time per call | Allocated per call |
| --- | ---: | ---: |
| Flat valid JSON validation | 2.4 µs | 2.1 KiB |
| Flat JSON SafeParse | 2.5 µs | 2.4 KiB |
| 20 nested JSON rows | 37 µs | 30.3 KiB |
| 100 BOM rows with expanded Parts, existing IOM DOM | 296 µs | 184.1 KiB |
| Same tree with SafeParse | 326 µs | 234.0 KiB |

These measurements include validation and output allocation, exclude network/database time,
and use warm calls with tiered compilation disabled. Measure the payload sizes and host you
actually use. Text AML also includes document construction and inspection.

## Practical choices

Keep input and collection limits close to real Method requirements. Use `FirstIssue` when a
single failure is sufficient. `ValidatedArray<T>.GetValues()` reuses a read-only view rather
than copying the collection on each call.

Callbacks run synchronously. Avoid I/O and shared mutable state inside predicates and transforms.
