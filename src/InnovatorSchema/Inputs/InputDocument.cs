using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Xml;

namespace InnovatorSchema;

internal sealed class InputDocument : IDisposable
{
    private readonly JsonDocument? json;

    private InputDocument(InputNode root, JsonDocument? json = null)
    {
        Root = root;
        this.json = json;
    }

    public InputNode Root { get; }

    public static InputDocument Missing()
    {
        return new InputDocument(MissingInputNode.Instance);
    }

    public static InputDocument Json(string text, ValidationOptions options)
    {
        CheckLength(text, options);
        JsonSourceGuard.Check(text, options);
        var settings = new JsonDocumentOptions
        {
            MaxDepth = options.MaximumDepth,
            CommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false
        };
        JsonDocument document = JsonDocument.Parse(text, settings);
        bool completed = false;

        try
        {
            new InputInspection(options).Json(document.RootElement);
            var result = new InputDocument(new JsonInputNode(document.RootElement), document);
            completed = true;
            return result;
        }
        finally
        {
            if (!completed)
            {
                document.Dispose();
            }
        }
    }

    public static InputDocument Xml(string text, ValidationOptions options)
    {
        CheckLength(text, options);
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = options.MaximumInputLength,
            IgnoreWhitespace = false
        };
        using var source = new StringReader(text);
        using var parser = XmlReader.Create(source, settings);
        using var reader = new BoundedXmlReader(parser, options);
        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        document.Load(reader);

        if (document.DocumentElement is not XmlElement root)
        {
            throw new XmlException("XML must have a document element.");
        }

        return FromXml(root, options);
    }

    public static InputDocument FromXml(XmlElement element, ValidationOptions options)
    {
        if (element.OwnerDocument.DocumentType is not null)
        {
            throw new XmlException("Document types are not supported.");
        }

        new InputInspection(options).Xml(element);
        return new InputDocument(new XmlInputNode(element));
    }

    public static InputDocument FromElements(
        IReadOnlyList<XmlElement> elements, ValidationOptions options)
    {
        new InputInspection(options).XmlSequence(elements);
        var nodes = new InputNode[elements.Count];

        for (int index = 0; index < nodes.Length; index++)
        {
            nodes[index] = new XmlInputNode(elements[index]);
        }

        return new InputDocument(new SequenceInputNode(nodes));
    }

    public void Dispose()
    {
        json?.Dispose();
        GC.SuppressFinalize(this);
    }

    private static void CheckLength(string text, ValidationOptions options)
    {
        if (text.Length > options.MaximumInputLength)
        {
            throw new InputFailureException(
                IssueCode.LimitExceeded,
                "Input text limit exceeded.",
                new IssuePath(Array.Empty<PathSegment>()));
        }
    }
}
