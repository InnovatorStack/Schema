namespace InnovatorSchema;

/// <summary>The exact tokens accepted by BooleanText; matching is case-sensitive.</summary>
public enum BooleanTextFormat
{
    /// <summary>Accept 0 and 1.</summary>
    ZeroOne,
    /// <summary>Accept false and true.</summary>
    TrueFalse,
    /// <summary>Accept 0, 1, false and true.</summary>
    Either
}

internal sealed class BooleanReader : ValueReader<bool>
{
    private readonly BooleanTextFormat? format;

    public BooleanReader(BooleanTextFormat? format) : base(DefinitionGraph.Leaf)
    {
        this.format = format;
    }

    protected override ValidationValue<bool> ReadValue(InputNode input, ValidationContext context)
    {
        if (!format.HasValue && input.Kind == InputKind.Boolean)
        {
            return ValidationValue<bool>.Present(input.Boolean);
        }

        if (!format.HasValue || input.Kind != InputKind.String)
        {
            context.Add(IssueCode.TypeMismatch, "The declared boolean representation is required.");
            return ValidationValue<bool>.Invalid();
        }

        string text = input.Text;
        bool numeric = format != BooleanTextFormat.TrueFalse;
        bool literal = format != BooleanTextFormat.ZeroOne;

        if ((numeric && text == "1") || (literal && text == "true"))
        {
            return ValidationValue<bool>.Present(true);
        }

        if ((numeric && text == "0") || (literal && text == "false"))
        {
            return ValidationValue<bool>.Present(false);
        }

        context.Add(IssueCode.InvalidBoolean, "Boolean text is not in the declared format.");
        return ValidationValue<bool>.Invalid();
    }
}
