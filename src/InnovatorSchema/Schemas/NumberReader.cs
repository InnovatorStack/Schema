namespace InnovatorSchema;

internal delegate bool TryReadNumber<T>(string text, out T value) where T : struct;

internal sealed class NumberReader<T> : ValueReader<T> where T : struct
{
    private readonly InputKind sourceKind;
    private readonly TryReadNumber<T> parse;

    public NumberReader(InputKind sourceKind, TryReadNumber<T> parse) : base(DefinitionGraph.Leaf)
    {
        this.sourceKind = sourceKind;
        this.parse = parse;
    }

    protected override ValidationValue<T> ReadValue(InputNode input, ValidationContext context)
    {
        if (input.Kind != sourceKind)
        {
            context.Add(IssueCode.TypeMismatch, "The declared numeric representation is required.");
            return ValidationValue<T>.Invalid();
        }

        if (!parse(input.Text, out T value))
        {
            context.Add(IssueCode.InvalidNumber,
                "Number cannot be represented exactly in this type.");
            return ValidationValue<T>.Invalid();
        }

        return ValidationValue<T>.Present(value);
    }
}
