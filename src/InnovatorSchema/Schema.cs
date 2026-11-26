namespace InnovatorSchema;

/// <summary>A rule for a declared AML value.</summary>
public abstract class Schema
{
    private protected Schema(bool allowsNullish, int depth)
    {
        AllowsNullish = allowsNullish;
        Depth = depth;
    }

    internal bool AllowsNullish { get; }
    internal int Depth { get; }

    /// <summary>Accept missing, null, empty and whitespace values.</summary>
    public abstract Schema Nullish();

    internal abstract object? Read(FieldInput input, ValidationContext context, bool capture);
}
