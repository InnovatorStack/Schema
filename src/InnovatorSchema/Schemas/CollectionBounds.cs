using System;

namespace InnovatorSchema;

internal readonly record struct CollectionBounds(int Minimum, int Maximum)
{
    public static CollectionBounds Default => new(0, Limits.Items);

    public CollectionBounds Min(int count)
    {
        if (count < 0 || count > Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        return new CollectionBounds(Math.Max(Minimum, count), Maximum);
    }

    public CollectionBounds Max(int count)
    {
        if (count < Minimum || count > Limits.Items)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        return new CollectionBounds(Minimum, Math.Min(Maximum, count));
    }

    public void Validate(int count, ValidationContext context, string unit)
    {
        if (count < Minimum)
        {
            context.Add(FormattableString.Invariant($"At least {Minimum} {unit} are required."));
        }
        else if (count > Maximum)
        {
            context.Add(FormattableString.Invariant($"At most {Maximum} {unit} are allowed."));
        }
    }
}
