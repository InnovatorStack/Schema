# Supported API

| Input | Single values | Objects | Collections |
| --- | --- | --- | --- |
| JSON | Strings, decimal, Int64, boolean | Declared properties | Arrays |
| XML | Explicit text readers | Elements and attributes | Child elements |
| AML | Item properties | Items and expanded references | AML roots and relationships |
| IOM | Supplied Item data | Borrowed Item DOM | Complete Item selections |

## Validation and parsing

- `Validate` returns typed validation views and issues.
- `SafeParse` returns runtime output or a structured error.
- `Parse` returns runtime output or throws `ValidationException`.

Object and Item schemas allow extra properties by default. Strict rejection is opt-in.
Definitions support presence policies, nested schemas, value rules, custom checks, typed
transforms and message overrides. All-issues collection is the default.

## Boundaries

Schemas validate supplied data. They do not fetch references, apply Items or validate a
partially supplied patch as though it were a complete object. Text readers use explicit,
culture-independent conversions. [Presence](/guides/presence), [limits](/guides/limits) and
[compatibility](/guides/compatibility) describe the input contracts.
