# API reference

Use `using InnovatorSchema;` and `using V = InnovatorSchema.Validator;` for these examples.
The current API validates explicitly selected source formats and returns structured results.
`Parse` and `SafeParse` create runtime objects with the fields declared by the schema.

## Schemas

- [String](/api/string)
- [Decimal](/api/decimal)
- [DecimalText](/api/decimal-text)
- [Integer](/api/integer)
- [IntegerText](/api/integer-text)
- [Boolean](/api/boolean)
- [BooleanText](/api/boolean-text)
- [Object](/api/object)
- [Array](/api/array)
- [Item](/api/item)

## Fields and shapes

- [Field](/api/field)
- [Attribute](/api/attribute)
- [Relationships](/api/relationships)
- [Property](/api/property)
- [UnknownProperties](/api/unknown-properties)

## Rules

- [NotBlank](/api/not-blank)
- [MinLength](/api/min-length)
- [MaxLength](/api/max-length)
- [Matches](/api/matches)
- [Min](/api/min)
- [Max](/api/max)
- [MinCount](/api/min-count)
- [MaxCount](/api/max-count)
- [Check](/api/check)
- [And](/api/and)

## Pipelines

- [Optional](/api/optional)
- [Nullable](/api/nullable)
- [RequireValue](/api/require-value)
- [Transform](/api/transform)
- [TransformNullable](/api/transform-nullable)
- [WithMessage](/api/with-message)

## Validation

- [Parse](/api/parse)
- [SafeParse](/api/safe-parse)
- [Validate](/api/validate)

- [ValidateJson](/api/validate-json)
- [ValidateXml](/api/validate-xml)
- [ValidateAml](/api/validate-aml)

## Results and options

- [SafeParseResult](/api/safe-parse-result)
- [ParsedObject](/api/parsed-object)
- [ValidationError](/api/validation-error)
- [ValidationException](/api/validation-exception)
- [InputFormat](/api/input-format)

- [ValidationResult](/api/validation-result)
- [ValidatedObject](/api/validated-object)
- [ValidatedArray](/api/validated-array)
- [ValidationValue](/api/validation-value)
- [ValidationIssue](/api/validation-issue)
- [IssuePath](/api/issue-path)
- [IssueCode](/api/issue-code)
- [ValidationOptions](/api/validation-options)
