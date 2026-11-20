namespace InnovatorSchema;

internal sealed class ArrayReader<T> : ValueReader<ValidatedArray<T>>
{
    private readonly Schema<T> item;

    public ArrayReader(Schema<T> item) : base(new DefinitionGraph(new[] { item.Graph }), true)
    {
        this.item = item;
    }

    protected override ValidationValue<ValidatedArray<T>> ReadValue(
        InputNode input, ValidationContext context)
    {
        if (!input.CanReadArray)
        {
            context.Add(IssueCode.TypeMismatch, "An array or explicit XML collection is required.");
            return ValidationValue<ValidatedArray<T>>.Invalid();
        }

        if (input.Elements.Count > context.Options.MaximumCollectionLength)
        {
            context.Limit("Collection length limit exceeded.");
            return ValidationValue<ValidatedArray<T>>.Invalid();
        }

        int before = context.IssueCount;
        var values = new ValidationValue<T>[input.Elements.Count];

        for (int index = 0; index < values.Length && !context.IsStopped; index++)
        {
            using var scope = context.Enter(
                new PathSegment(PathSegmentKind.Index, string.Empty, index));

            if (scope.IsEntered)
            {
                values[index] = item.Read(input.Elements[index], context);
            }
        }

        if (context.IssueCount != before || context.IsStopped)
        {
            return ValidationValue<ValidatedArray<T>>.Invalid();
        }

        return ValidationValue<ValidatedArray<T>>.Present(new ValidatedArray<T>(values));
    }
}
