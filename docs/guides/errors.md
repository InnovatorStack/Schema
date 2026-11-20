# Handle errors

Validation collects all independent issues by default. Set `ValidationMode.FirstIssue` to
stop early. Issue and resource limits still apply.

## Parse

```csharp
try
{
    var data = V.Parse(schema, json);
}
catch (ValidationException error)
{
    string message = error.GetErrorString();
    var issues = error.GetIssues();
}
```

`Parse` throws `InnovatorSchema.ValidationException` for validation failures. Invalid API
arguments retain `ArgumentException` subtypes. Cancellation propagates as
`OperationCanceledException`. Exceptions from your predicates and transforms propagate
unchanged; `SafeParse` does not hide programming errors.

## SafeParse

```csharp
var result = V.SafeParse(schema, json);

if (!result.Success)
{
    var error = result.Error;
    string message = error.GetErrorString();
    var issues = error.GetIssues();
    var first = issues[0];
}
```

Each issue has a `Code`, `Message`, `Path` and optional `RuleId`. Paths include nested fields
and collection indexes. Parser errors can also include source coordinates.

A summary looks like:

```text
Schema validation failed.
Problems:
- $.name: Use at most ten characters.
- $.age: Enter a positive age.
```

## Aras errors

Pass the Item explicitly:

```csharp
var result = V.SafeParse(schema, this);

if (!result.Success)
{
    return this.getInnovator().newError(result.Error.GetErrorString());
}
```

The caller creates the IOM error Item. Schema returns a plain `ValidationError` and retains
no Innovator instance.

## Field messages

```csharp
var code = V.String()
    .WithMessage(IssueCode.Required, "Enter an item number.")
    .NotBlank("Enter an item number.")
    .MaxLength(64, "Use at most 64 characters.");
```

`WithMessage` overrides reader and presence diagnostics on that schema. Rule messages apply
to their own checks. Neither changes child schemas.

## Custom rules

```csharp
var code = V.String().Check(
    value => value.StartsWith("P-", StringComparison.Ordinal),
    "part.prefix",
    "Start with P-.");
```

The issue has code `Custom` and rule id `part.prefix`. Whole-object predicates report at
the object's path.
