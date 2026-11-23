# Server Methods

[Install Schema](/guides/installation), then pass the current Item to `AssertInput`.

<<< ../../examples/PartMethod.cs

Use an Item from another IOM call the same way. The validator reads the supplied Item in
place, so the Method can still use `getProperty`, `getPropertyItem` and `getID` afterward.
