namespace InnovatorSchema;

/// <summary>A rule for a declared AML value.</summary>
public abstract class Schema
{
    private protected Schema(Presence presence, int depth)
    {
        FieldPresence = presence;
        Depth = depth;
    }

    private protected Presence FieldPresence { get; }
    internal int Depth { get; }

    /// <summary>Allow a missing or null field.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716",
        Justification = "Optional is the C# Method builder name.")]
    public abstract Schema Optional();

    /// <summary>Accept missing, null and blank values as null.</summary>
    [System.Obsolete("Use Optional() for missing or null fields and to preserve empty strings.")]
    public abstract Schema Nullish();

    internal abstract object? Read(FieldInput input, ValidationContext context, bool capture);

    private protected enum Presence
    {
        Required,
        Optional,
        LegacyNullish
    }
}
