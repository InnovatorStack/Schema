using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Xml;

namespace InnovatorSchema;

internal sealed class InputInspection
{
    private readonly ValidationOptions options;
    private readonly List<PathSegment> path = new();
    private int nodes;
    private long textLength;

    public InputInspection(ValidationOptions options)
    {
        this.options = options;
    }

    public void Json(JsonElement element, int depth = 1)
    {
        Visit(depth);

        if (element.ValueKind == JsonValueKind.Object)
        {
            JsonObject(element, depth);
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            if (element.GetArrayLength() > options.MaximumCollectionLength)
            {
                Fail(IssueCode.LimitExceeded, "Collection length limit exceeded.");
            }

            int index = 0;

            foreach (JsonElement child in element.EnumerateArray())
            {
                Push(new PathSegment(PathSegmentKind.Index, string.Empty, index));
                Json(child, depth + 1);
                path.RemoveAt(path.Count - 1);
                index++;
            }
        }
    }

    public void Xml(XmlNode node, int depth = 1)
    {
        Visit(depth);

        if (node is XmlElement element)
        {
            CheckXmlElement(element);
        }
        else if (node is XmlText or XmlCDataSection or XmlWhitespace or XmlSignificantWhitespace)
        {
            AddText(node.Value?.Length ?? 0);
        }

        int elementCount = 0;

        for (XmlNode? child = node.FirstChild; child is not null;
                child = child.NextSibling)
        {
            if (child is XmlElement)
            {
                elementCount++;
            }

            if (elementCount > options.MaximumCollectionLength)
            {
                Fail(IssueCode.LimitExceeded, "Collection length limit exceeded.");
            }

            int childDepth = depth;

            if (child is XmlElement)
            {
                childDepth++;
                Push(new PathSegment(PathSegmentKind.Property, child.LocalName));
            }

            Xml(child, childDepth);

            if (child is XmlElement)
            {
                path.RemoveAt(path.Count - 1);
            }
        }
    }

    public void XmlSequence(IReadOnlyList<XmlElement> elements)
    {
        Visit(1);

        if (elements.Count > options.MaximumCollectionLength)
        {
            Fail(IssueCode.LimitExceeded, "Collection length limit exceeded.");
        }

        for (int index = 0; index < elements.Count; index++)
        {
            Push(new PathSegment(PathSegmentKind.Index, string.Empty, index));

            if (elements[index].OwnerDocument.DocumentType is not null)
            {
                Fail(IssueCode.MalformedXml, "Document types are not supported.");
            }

            Xml(elements[index], 2);
            path.RemoveAt(path.Count - 1);
        }
    }

    private void JsonObject(JsonElement element, int depth)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (JsonProperty property in element.EnumerateObject())
        {
            Push(new PathSegment(PathSegmentKind.Property, property.Name));

            if (!names.Add(property.Name))
            {
                Fail(IssueCode.DuplicateProperty, "Property occurs more than once.");
            }

            if (names.Count > options.MaximumCollectionLength)
            {
                Fail(IssueCode.LimitExceeded, "Object member limit exceeded.");
            }

            Json(property.Value, depth + 1);
            path.RemoveAt(path.Count - 1);
        }
    }

    private void CheckXmlElement(XmlElement element)
    {
        AddText(element.Name.Length);

        foreach (XmlAttribute attribute in element.Attributes)
        {
            Visit(1);
            AddText(attribute.Name.Length + attribute.Value.Length);
        }
    }

    private void Visit(int depth)
    {
        options.CancellationToken.ThrowIfCancellationRequested();
        nodes++;

        if (nodes > options.MaximumNodes || depth > options.MaximumDepth)
        {
            Fail(IssueCode.LimitExceeded, "Input node or nesting limit exceeded.");
        }
    }

    private void AddText(int length)
    {
        textLength += length;

        if (textLength > options.MaximumInputLength)
        {
            Fail(IssueCode.LimitExceeded, "Input text limit exceeded.");
        }
    }

    private void Push(PathSegment segment)
    {
        if (segment.Name.Length > 1024)
        {
            Fail(IssueCode.LimitExceeded, "Input property name exceeds 1024 characters.");
        }

        int length = segment.Name.Length;

        foreach (PathSegment existing in path)
        {
            length += existing.Name.Length + 16;
        }

        if (length > 4096)
        {
            Fail(IssueCode.LimitExceeded, "Input path length limit exceeded.");
        }

        path.Add(segment);
    }

    private void Fail(IssueCode code, string message)
    {
        throw new InputFailureException(code, message, new IssuePath(path));
    }
}
