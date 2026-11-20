using System;
using System.Xml;

namespace InnovatorSchema;

internal sealed class BoundedXmlReader : XmlReader
{
    private readonly XmlReader source;
    private readonly ValidationOptions options;
    private int nodes;

    public BoundedXmlReader(XmlReader source, ValidationOptions options)
    {
        this.source = source;
        this.options = options;
    }

    public override bool Read()
    {
        options.CancellationToken.ThrowIfCancellationRequested();
        bool read = source.Read();

        if (!read)
        {
            return false;
        }

        if (source.NodeType != XmlNodeType.EndElement)
        {
            nodes += 1 + source.AttributeCount;
        }

        int depth = source.Depth + 1;

        if (source.NodeType is XmlNodeType.Text or XmlNodeType.CDATA or
            XmlNodeType.Whitespace or XmlNodeType.SignificantWhitespace)
        {
            depth = source.Depth;
        }

        if (depth > options.MaximumDepth || nodes > options.MaximumNodes)
        {
            throw new InputFailureException(
                IssueCode.LimitExceeded,
                "XML node or nesting limit exceeded.",
                new IssuePath(Array.Empty<PathSegment>()));
        }

        return true;
    }

    public override int AttributeCount => source.AttributeCount;
    public override string BaseURI => source.BaseURI;
    public override int Depth => source.Depth;
    public override bool EOF => source.EOF;
    public override bool HasValue => source.HasValue;
    public override bool IsEmptyElement => source.IsEmptyElement;
    public override string LocalName => source.LocalName;
    public override string Name => source.Name;
    public override XmlNameTable NameTable => source.NameTable;
    public override string NamespaceURI => source.NamespaceURI;
    public override XmlNodeType NodeType => source.NodeType;
    public override string Prefix => source.Prefix;
    public override ReadState ReadState => source.ReadState;
    public override string Value => source.Value;

    public override string? GetAttribute(string name)
    {
        return source.GetAttribute(name);
    }

    public override string? GetAttribute(string name, string? namespaceURI)
    {
        return source.GetAttribute(name, namespaceURI);
    }

    public override string GetAttribute(int index)
    {
        return source.GetAttribute(index);
    }

    public override string? LookupNamespace(string prefix)
    {
        return source.LookupNamespace(prefix);
    }

    public override bool MoveToAttribute(string name)
    {
        return source.MoveToAttribute(name);
    }

    public override bool MoveToAttribute(string name, string? namespaceURI)
    {
        return source.MoveToAttribute(name, namespaceURI);
    }

    public override bool MoveToElement()
    {
        return source.MoveToElement();
    }

    public override bool MoveToFirstAttribute()
    {
        return source.MoveToFirstAttribute();
    }

    public override bool MoveToNextAttribute()
    {
        return source.MoveToNextAttribute();
    }

    public override bool ReadAttributeValue()
    {
        return source.ReadAttributeValue();
    }

    public override void ResolveEntity()
    {
        source.ResolveEntity();
    }

    public override void Close()
    {
        source.Close();
    }
}
