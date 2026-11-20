namespace InnovatorSchema;

internal enum InputKind
{
    Missing,
    Null,
    String,
    Number,
    Boolean,
    Object,
    Array
}

/// <summary>The representation used by a text validation or parse call.</summary>
public enum InputFormat
{
    /// <summary>JSON values, objects and arrays.</summary>
    Json,
    /// <summary>XML elements, attributes and explicit collections.</summary>
    Xml,
    /// <summary>Aras Items, optionally enclosed in an AML element.</summary>
    Aml
}
