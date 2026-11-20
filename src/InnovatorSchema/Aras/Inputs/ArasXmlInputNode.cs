using System.Collections.Generic;
using System.Xml;

namespace InnovatorSchema;

internal sealed class ArasXmlInputNode : XmlInputNode
{
    public ArasXmlInputNode(XmlElement element) : base(element)
    {
    }

    public override InputKind Kind => Element.GetAttribute("is_null") == "1" ?
        InputKind.Null : base.Kind;

    public override bool Validate(ValidationContext context)
    {
        if (!Element.HasAttribute("is_null"))
        {
            return true;
        }

        string marker = Element.GetAttribute("is_null");

        if (marker != "0" && marker != "1")
        {
            context.Add(IssueCode.InvalidBoolean, "is_null must be 0 or 1.");
            return false;
        }

        if (marker == "1" && Element.HasChildNodes)
        {
            context.Add(IssueCode.NullNotAllowed, "A null element cannot contain content.");
            return false;
        }

        return true;
    }

    public InputNode ExpandedItem()
    {
        if (Element.Name == "Item" || Kind == InputKind.Null || !CanReadObject)
        {
            return this;
        }

        if (Elements.Count == 1 && Elements[0] is ArasXmlInputNode child &&
            child.Element.Name == "Item" && child.Element.NamespaceURI.Length == 0)
        {
            return child;
        }

        return this;
    }

    public override InputNode Collection(
        string container, string item, string attribute, string value)
    {
        return new RelationshipInputNode(Element, container, item, attribute, value);
    }

    protected override XmlInputNode CreateNode(XmlElement child)
    {
        return new ArasXmlInputNode(child);
    }
}

internal sealed class RelationshipInputNode : InputNode
{
    private readonly XmlElement? container;
    private readonly string itemName;

    public RelationshipInputNode(
        XmlElement parent, string containerName, string itemName, string attribute, string value)
    {
        this.itemName = itemName;
        var elements = new List<InputNode>();

        for (XmlNode? child = parent.FirstChild; child is not null;
                child = child.NextSibling)
        {
            if (child is XmlElement group && group.Name == containerName &&
                group.NamespaceURI.Length == 0)
            {
                container = group;
                break;
            }
        }

        if (container is not null)
        {
            for (XmlNode? child = container.FirstChild; child is not null;
                child = child.NextSibling)
            {
                if (child is XmlElement row && row.Name == itemName &&
                    row.NamespaceURI.Length == 0 && row.GetAttribute(attribute) == value)
                {
                    elements.Add(new ArasXmlInputNode(row));
                }
            }
        }

        Elements = elements.AsReadOnly();
    }

    public override InputKind Kind => container?.GetAttribute("is_null") == "1" ?
        InputKind.Null : InputKind.Array;

    public override InputFormat Format => InputFormat.Xml;

    public override IReadOnlyList<InputNode> Elements { get; }

    public override bool Validate(ValidationContext context)
    {
        if (container is null)
        {
            return true;
        }

        var source = new ArasXmlInputNode(container);

        if (!source.Validate(context))
        {
            return false;
        }

        if (source.Kind == InputKind.Null)
        {
            return true;
        }

        if (!source.CanReadArray)
        {
            context.Add(IssueCode.TypeMismatch, "Relationships must contain Items.");
            return false;
        }

        for (XmlNode? child = container.FirstChild; child is not null;
                child = child.NextSibling)
        {
            if (child is XmlElement row &&
                (row.Name != itemName || row.NamespaceURI.Length != 0 ||
                string.IsNullOrWhiteSpace(row.GetAttribute("type"))))
            {
                context.Add(IssueCode.TypeMismatch, "Relationships must contain typed Items.");
                return false;
            }
        }

        return true;
    }
}
