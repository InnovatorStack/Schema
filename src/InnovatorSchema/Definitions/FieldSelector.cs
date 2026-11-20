namespace InnovatorSchema;

internal enum FieldSource
{
    Property,
    Attribute,
    Relationship
}

internal sealed class FieldSelector
{
    public FieldSelector(FieldSource source, string name)
    {
        Source = source;
        Name = name;
        OutputName = source == FieldSource.Attribute ? "@" + name : name;
    }

    public FieldSource Source { get; }
    public string Name { get; }
    public string OutputName { get; }
    public string Key => Source + ":" + Name;

    public PathSegment Segment => Source switch
    {
        FieldSource.Attribute => new PathSegment(PathSegmentKind.Attribute, Name),
        FieldSource.Relationship => new PathSegment(PathSegmentKind.Relationship, Name),
        _ => new PathSegment(PathSegmentKind.Property, Name)
    };

    public InputNode Select(InputNode input)
    {
        return Source switch
        {
            FieldSource.Attribute => input.Attribute(Name),
            FieldSource.Relationship => input.Collection("Relationships", "Item", "type", Name),
            _ => input.Property(Name)
        };
    }
}
