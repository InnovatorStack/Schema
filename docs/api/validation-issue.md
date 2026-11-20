# ValidationIssue

Describe one deterministic validation or input failure.

## Signature

```csharp
IssueCode issue.Code
string issue.Message
IssuePath issue.Path
string? issue.RuleId
int? issue.LineNumber
int? issue.ColumnNumber
```

## Example

```csharp
foreach (var issue in result.Issues)
{
    string path = issue.Path.ToString();
    string message = issue.Message;
}
```

## Behavior

RuleId is populated for custom rules. Source positions are available for some parser failures, not ordinary semantic field failures. Positions are one-based. JSON columns currently follow the parser's UTF-8 byte offset; XML columns follow the XML parser. Do not infer field identity by parsing Message; use Code and Path.
