# Validate a Part collection

Require at least one Part and check every row. The first row has index `0`.

<<< ../../examples/PartBatchMethod.cs

## Parse BOM rows

Read quantities and the weight of each related Part, then return the weight per line
and the total. A reference designator is optional.

<<< ../../examples/ParseBomMethod.cs
