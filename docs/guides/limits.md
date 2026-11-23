# Input limits

The validator bounds the amount of work one assertion can do. An assertion stops
when it exceeds its work or issue budget.

| Input | Limit |
| --- | --- |
| AML text length | 1 Mi characters |
| AML XML nesting | 64 levels |
| Items in a collection | 10,000 |
| Declared fields per Item | 128 |
| Reported validation issues | 100, followed by a limit issue |

Split a larger import into batches before validating it.
