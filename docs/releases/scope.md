# Supported API

Schema asserts AML strings and IOM Items. It checks Item type, ID, attributes, properties,
expanded Items, and collections. Field rules cover strings, enums, integers, doubles,
dates, Aras GUIDs, numeric bounds and text length.

Version 0.2 replaces the 0.1 parsing API with `AssertInput`. Server Methods use the
original Item after the assertion succeeds.
