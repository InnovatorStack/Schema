using System;
using System.Collections.Generic;
using System.Xml;

namespace InnovatorSchema;

internal class XmlInputNode : InputNode
{
    private readonly XmlElement element;
    private IReadOnlyList<InputMember>? members;
    private IReadOnlyList<InputNode>? elements;
    private Dictionary<string, InputNode>? index;
    private SortedSet<string>? duplicates;
    private InputKind? kind;
    private bool? scalarContent;

    public XmlInputNode(XmlElement element)
    {
        this.element = element;
    }

    internal XmlElement Element => element;

    public override InputFormat Format => InputFormat.Xml;

    public override InputKind Kind
    {
        get
        {
            if (kind.HasValue)
            {
                return kind.Value;
            }

            for (XmlNode? child = element.FirstChild; child is not null;
                child = child.NextSibling)
            {
                if (child is XmlElement)
                {
                    kind = InputKind.Object;
                    return InputKind.Object;
                }
            }

            kind = InputKind.String;
            return InputKind.String;
        }
    }

    public override bool CanReadObject => Kind != InputKind.Null && !HasScalarContent();

    public override bool CanReadArray => Kind != InputKind.Null && !HasScalarContent();

    public override string Text => element.InnerText;

    public override IReadOnlyList<InputMember> Members
    {
        get
        {
            EnsureMembers();
            return members ?? throw new InvalidOperationException("Members were not initialized.");
        }
    }

    public override IReadOnlyList<InputNode> Elements
    {
        get
        {
            if (elements is null)
            {
                var values = new List<InputNode>();

                for (XmlNode? child = element.FirstChild; child is not null;
                child = child.NextSibling)
                {
                    if (child is XmlElement item)
                    {
                        values.Add(CreateNode(item));
                    }
                }

                elements = values.AsReadOnly();
            }

            return elements;
        }
    }

    public override IReadOnlySet<string>? DuplicateProperties
    {
        get
        {
            EnsureMembers();
            return duplicates;
        }
    }

    public override InputNode Property(string name)
    {
        EnsureMembers();

        if (index is not null && index.TryGetValue(name, out InputNode? value))
        {
            return value;
        }

        return MissingInputNode.Instance;
    }

    public override InputNode Attribute(string name)
    {
        if (element.HasAttribute(name))
        {
            return new TextInputNode(element.GetAttribute(name));
        }

        return MissingInputNode.Instance;
    }

    public override InputNode Collection(
        string container, string item, string attribute, string value)
    {
        var values = new List<InputNode>();

        for (XmlNode? child = element.FirstChild; child is not null;
                child = child.NextSibling)
        {
            if (child is not XmlElement group || group.Name != container ||
                group.NamespaceURI.Length != 0)
            {
                continue;
            }

            for (XmlNode? nested = group.FirstChild; nested is not null;
                nested = nested.NextSibling)
            {
                if (nested is XmlElement candidate && candidate.Name == item &&
                    candidate.NamespaceURI.Length == 0 &&
                    candidate.GetAttribute(attribute) == value)
                {
                    values.Add(CreateNode(candidate));
                }
            }
        }

        return new SequenceInputNode(values.AsReadOnly());
    }

    private void EnsureMembers()
    {
        if (members is not null)
        {
            return;
        }

        var values = new List<InputMember>();
        index = new Dictionary<string, InputNode>(StringComparer.Ordinal);

        for (XmlNode? child = element.FirstChild; child is not null;
                child = child.NextSibling)
        {
            if (child is not XmlElement property)
            {
                continue;
            }

            string name = property.LocalName;

            if (property.NamespaceURI.Length != 0)
            {
                name = "{" + property.NamespaceURI + "}" + name;
            }

            XmlInputNode node = CreateNode(property);
            values.Add(new InputMember(name, node));
            if (!index.TryAdd(name, node))
            {
                duplicates ??= new SortedSet<string>(StringComparer.Ordinal);
                duplicates.Add(name);
            }
        }

        members = values.AsReadOnly();
    }

    protected virtual XmlInputNode CreateNode(XmlElement child)
    {
        return new XmlInputNode(child);
    }

    protected bool HasScalarContent()
    {
        if (scalarContent.HasValue)
        {
            return scalarContent.Value;
        }

        for (XmlNode? child = element.FirstChild; child is not null;
                child = child.NextSibling)
        {
            if (child is XmlText or XmlCDataSection && !string.IsNullOrWhiteSpace(child.Value))
            {
                scalarContent = true;
                return true;
            }
        }

        scalarContent = false;
        return false;
    }
}
