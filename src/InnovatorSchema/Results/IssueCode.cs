namespace InnovatorSchema;

/// <summary>Stable categories for input, validation and resource failures.</summary>
public enum IssueCode
{
    /// <summary>A required value is missing.</summary>
    Required,
    /// <summary>Explicit null is not accepted.</summary>
    NullNotAllowed,
    /// <summary>The source representation has the wrong type.</summary>
    TypeMismatch,
    /// <summary>A string is empty or contains only whitespace.</summary>
    NotBlank,
    /// <summary>A length is below its minimum.</summary>
    MinimumLength,
    /// <summary>A length exceeds its maximum.</summary>
    MaximumLength,
    /// <summary>A value is below an inclusive minimum.</summary>
    Minimum,
    /// <summary>A value exceeds an inclusive maximum.</summary>
    Maximum,
    /// <summary>A regular expression did not match.</summary>
    Pattern,
    /// <summary>A custom rule failed; RuleId identifies the rule.</summary>
    Custom,
    /// <summary>JSON syntax is malformed.</summary>
    MalformedJson,
    /// <summary>XML syntax is malformed or prohibited.</summary>
    MalformedXml,
    /// <summary>A property occurs more than once.</summary>
    DuplicateProperty,
    /// <summary>A property is not declared by a strict object.</summary>
    UnknownProperty,
    /// <summary>The number of supplied root Items is unexpected.</summary>
    Cardinality,
    /// <summary>A parsing or execution resource limit was reached.</summary>
    LimitExceeded,
    /// <summary>A numeric representation cannot be interpreted exactly.</summary>
    InvalidNumber,
    /// <summary>A boolean representation is not accepted by the selected format.</summary>
    InvalidBoolean,
    /// <summary>An Item has an unexpected type.</summary>
    ItemType,
    /// <summary>A nested schema requires an expanded Item.</summary>
    ExpansionRequired,
    /// <summary>A collection contains fewer entries than its minimum.</summary>
    MinimumCount,
    /// <summary>A collection contains more entries than its maximum.</summary>
    MaximumCount
}
