namespace InnovatorSchema;

/// <summary>A collection in which every supplied Item must satisfy the same schema.</summary>
public sealed class ItemsSchema
{
    private readonly CollectionBounds bounds;

    internal ItemsSchema(ItemSchema item) : this(item, CollectionBounds.Default)
    {
    }

    private ItemsSchema(ItemSchema item, CollectionBounds bounds)
    {
        Item = item;
        this.bounds = bounds;
    }

    internal ItemSchema Item { get; }

    /// <summary>Require at least this many Items.</summary>
    public ItemsSchema Min(int count)
    {
        return new ItemsSchema(Item, bounds.Min(count));
    }

    /// <summary>Allow at most this many Items.</summary>
    public ItemsSchema Max(int count)
    {
        return new ItemsSchema(Item, bounds.Max(count));
    }

    internal void ValidateCount(int count, ValidationContext context)
    {
        bounds.Validate(count, context, "Items");
    }
}
