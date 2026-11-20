namespace InnovatorSchema;

/// <summary>The source role of a path segment.</summary>
public enum PathSegmentKind
{
    /// <summary>An object property.</summary>
    Property,
    /// <summary>A collection index.</summary>
    Index,
    /// <summary>An XML attribute.</summary>
    Attribute,
    /// <summary>A selected relationship type.</summary>
    Relationship
}

/// <summary>
/// One property, index, attribute or relationship component of a validation path.
/// </summary>
public readonly struct PathSegment
{
    private readonly string? name;

    internal PathSegment(PathSegmentKind kind, string name, int index = 0)
    {
        Kind = kind;
        this.name = name;
        Index = index;
    }

    /// <summary>The role of this component.</summary>
    public PathSegmentKind Kind { get; }

    /// <summary>The exact property, attribute or relationship name.</summary>
    public string Name => name ?? string.Empty;

    /// <summary>The zero-based index when Kind is Index.</summary>
    public int Index { get; }
}
