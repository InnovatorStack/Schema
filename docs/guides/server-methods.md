# Server Methods

[Install Schema](/guides/installation), then pass the current Item to `AssertInput`.

<<< ../../examples/PartMethod.cs

Use an Item from another IOM call the same way. The validator reads the supplied Item in
place, so the Method can still use `getProperty`, `getPropertyItem` and `getID` afterward.

Use `ParseInput` when the Method needs the converted values. You can read a field
by name or with dot syntax.

<<< ../../examples/ParsePartMethod.cs
