namespace InnovatorSchema;

/// <summary>One validation failure with a code, message and logical path.</summary>
public sealed class ValidationIssue
{
    internal ValidationIssue(
        IssueCode code,
        string message,
        IssuePath path,
        string? ruleId = null,
        int? lineNumber = null,
        int? columnNumber = null)
    {
        Code = code;
        Message = message;
        Path = path;
        RuleId = ruleId;
        LineNumber = lineNumber;
        ColumnNumber = columnNumber;
    }

    /// <summary>The failure category; custom rules also supply RuleId.</summary>
    public IssueCode Code { get; }

    /// <summary>The diagnostic text, including any caller override.</summary>
    public string Message { get; }

    /// <summary>The exact logical location of the failure.</summary>
    public IssuePath Path { get; }

    /// <summary>The caller's identifier when a custom rule fails.</summary>
    public string? RuleId { get; }

    /// <summary>A one-based source line, when the parser supplies one.</summary>
    public int? LineNumber { get; }

    /// <summary>A one-based parser position: UTF-8 bytes for JSON, characters for XML.</summary>
    public int? ColumnNumber { get; }
}
