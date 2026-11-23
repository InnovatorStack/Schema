using System;

namespace InnovatorSchema;

/// <summary>A collection in which every supplied Item must satisfy the same schema.</summary>
public sealed class ItemArraySchema
{
    private readonly int minimum;
    private readonly int maximum;

    internal ItemArraySchema(ItemSchema item, int minimum = 0, int maximum = Limits.Items)
    {
        Item = item;
        this.minimum = minimum;
        this.maximum = maximum;
    }

    internal ItemSchema Item { get; }

    /// <summary>Require at least this many Items.</summary>
    public ItemArraySchema MinItems(int count)
    {
        if (count < 0 || count > maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        return new ItemArraySchema(Item, Math.Max(minimum, count), maximum);
    }

    /// <summary>Allow at most this many Items.</summary>
    public ItemArraySchema MaxItems(int count)
    {
        if (count < minimum || count > Limits.Items)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        return new ItemArraySchema(Item, minimum, Math.Min(maximum, count));
    }

    internal void ValidateCount(int count, ValidationContext context)
    {
        if (count < minimum)
        {
            context.Add(FormattableString.Invariant($"At least {minimum} Items are required."));
        }
        else if (count > maximum)
        {
            context.Add(FormattableString.Invariant($"At most {maximum} Items are allowed."));
        }
    }
}
