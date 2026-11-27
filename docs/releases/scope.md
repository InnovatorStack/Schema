# Supported API

Schema asserts AML strings and IOM Items. It checks Item type, ID, attributes, properties,
expanded Items, and collections. `ParseInput` returns the parsed values and the
Innovator instance for an IOM Item. Field rules cover strings, booleans, integers,
doubles, dates, Aras GUIDs, allowed values, numeric bounds and text length.

Version 0.2 replaces the 0.1 parsing API with `AssertInput`. Server Methods use the
original Item after the assertion succeeds.
