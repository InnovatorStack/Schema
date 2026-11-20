# IssueCode

Stable categories for input, validation and resource failures.

## Signature

```csharp
IssueCode
```

## Example

```csharp
bool missing = result.Issues[0].Code == IssueCode.Required;
```

## Behavior

Presence: Required, NullNotAllowed. Representation: TypeMismatch, InvalidNumber, InvalidBoolean. Values: NotBlank, MinimumLength, MaximumLength, Minimum, Maximum, Pattern, Custom. Input: MalformedJson, MalformedXml, DuplicateProperty, UnknownProperty. Structure: Cardinality, ItemType, ExpansionRequired, MinimumCount, MaximumCount. Bounds: LimitExceeded. Code is independent of customizable message text.
