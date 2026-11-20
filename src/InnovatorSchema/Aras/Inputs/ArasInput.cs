using System;
using System.Collections.Generic;
using System.Xml;

namespace InnovatorSchema;

internal static class ArasInput
{
    public static ValidationResult<T> Read<T>(Schema<T> schema, object input,
        ValidationOptions? options)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(input);
        options ??= new ValidationOptions();
        options.CancellationToken.ThrowIfCancellationRequested();
        ItemXml source = ItemXml.Read(input);

        if (input is XmlElement or XmlDocument && source.Node is XmlElement root)
        {
            return ValidationEngine.Element(schema, root, options,
                schema.Reader.ReadsCollection ? AmlCollection : SingleAml);
        }

        return ValidationEngine.Execute(schema,
            () => Document(source, schema.Reader.ReadsCollection, options), options, Prepare);
    }

    private static XmlElement? Single(ItemXml item)
    {
        if (item.IsError)
        {
            return null;
        }

        if (item.Nodes is XmlNodeList nodes)
        {
            return nodes.Count == 1 && nodes[0] is XmlElement selected &&
                IsElement(selected, "Item") ? selected : null;
        }

        if (item.Node is XmlElement single && IsElement(single, "Item"))
        {
            return single;
        }

        return null;
    }

    private static InputDocument Document(ItemXml item, bool collection, ValidationOptions options)
    {
        if (!collection)
        {
            XmlElement? single = Single(item);

            if (single is null)
            {
                throw Failure(IssueCode.Cardinality, "Exactly one non-error Item is required.");
            }

            return InputDocument.FromXml(single, options);
        }

        if (item.IsError)
        {
            throw Failure(IssueCode.Cardinality, "An error Item is not an Item collection.");
        }

        var elements = new List<XmlElement>();

        if (item.Nodes is XmlNodeList nodes)
        {
            if (nodes.Count > options.MaximumCollectionLength)
            {
                throw Failure(IssueCode.LimitExceeded, "Collection length limit exceeded.");
            }

            foreach (XmlNode node in nodes)
            {
                if (node is not XmlElement element || !IsElement(element, "Item"))
                {
                    throw Failure(IssueCode.TypeMismatch, "The collection must contain Items.");
                }

                elements.Add(element);
            }
        }
        else if (item.Node is XmlElement element && IsElement(element, "Item"))
        {
            elements.Add(element);
        }
        else if (item.Document?.DocumentElement is XmlElement envelope &&
            IsElement(envelope, "AML"))
        {
            return InputDocument.FromXml(envelope, options);
        }
        else
        {
            throw Failure(IssueCode.Cardinality, "An Item or Item collection is required.");
        }

        return InputDocument.FromElements(elements, options);
    }

    public static InputNode SingleAml(InputNode input, ValidationContext context)
    {
        input = Prepare(input, context);

        if (!input.Validate(context))
        {
            return input;
        }

        if (input is not XmlInputNode xml)
        {
            context.Add(IssueCode.TypeMismatch, "AML requires an XML Item or AML root.");
            return input;
        }

        if (IsElement(xml.Element, "Item"))
        {
            return input;
        }

        if (!IsElement(xml.Element, "AML") || !input.CanReadArray)
        {
            context.Add(IssueCode.TypeMismatch, "AML requires an Item or AML root.");
            return input;
        }

        if (input.Elements.Count != 1)
        {
            context.Add(IssueCode.Cardinality, "Exactly one root Item is required.");
            return input;
        }

        if (input.Elements[0] is not XmlInputNode item || !IsElement(item.Element, "Item"))
        {
            context.Add(IssueCode.TypeMismatch, "AML must contain Item elements.");
            return input;
        }

        return item;
    }

    public static InputNode Prepare(InputNode input, ValidationContext context)
    {
        if (input is XmlInputNode xml)
        {
            return new ArasXmlInputNode(xml.Element);
        }

        if (input is SequenceInputNode sequence)
        {
            var values = new InputNode[sequence.Elements.Count];

            for (int index = 0; index < values.Length; index++)
            {
                values[index] = Prepare(sequence.Elements[index], context);
            }

            return new SequenceInputNode(values);
        }

        return input;
    }

    public static InputNode AmlCollection(InputNode input, ValidationContext context)
    {
        input = Prepare(input, context);

        if (!input.Validate(context))
        {
            return input;
        }

        if (input is ArasXmlInputNode single && IsElement(single.Element, "Item"))
        {
            return new SequenceInputNode(new[] { input });
        }

        if (input is not ArasXmlInputNode xml || !IsElement(xml.Element, "AML") ||
            !input.CanReadArray)
        {
            context.Add(IssueCode.TypeMismatch, "An Item or AML collection is required.");
            return input;
        }

        for (int index = 0; index < input.Elements.Count && !context.IsStopped; index++)
        {
            if (input.Elements[index] is not ArasXmlInputNode child ||
                !IsElement(child.Element, "Item"))
            {
                using var scope = context.Enter(
                    new PathSegment(PathSegmentKind.Index, string.Empty, index));
                context.Add(IssueCode.TypeMismatch, "AML must contain Item elements.");
            }
        }

        return new SequenceInputNode(input.Elements);
    }

    private static InputFailureException Failure(IssueCode code, string message)
    {
        return new InputFailureException(code, message,
            new IssuePath(Array.Empty<PathSegment>()));
    }

    private static bool IsElement(XmlElement element, string name)
    {
        return element.Name == name && element.NamespaceURI.Length == 0;
    }
}
