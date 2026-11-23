# Introduction

Schema checks AML input before a Server Method uses it. Describe the Item type, attributes,
and properties the Method needs, then call `V.AssertInput(schema, this)`. The Method keeps
working with the same IOM Item afterward.

Use the same schema with AML text when testing payloads outside Aras.
