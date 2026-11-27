using System.Xml;

namespace InnovatorSchema;

internal readonly record struct FieldInput(
    string? Text, XmlElement? Element, IomItem? Owner)
{
    public bool TryReadNull(ValidationContext context, out bool isNull)
    {
        isNull = false;

        if (Element?.GetAttributeNode("is_null") is not XmlAttribute marker)
        {
            return true;
        }

        if (marker.Value == "0")
        {
            return true;
        }

        if (marker.Value != "1")
        {
            context.AddMessage("is_null must be 0 or 1.",
                "Check whether this value should be blank.", includeField: true);
            return false;
        }

        for (XmlNode? child = Element.FirstChild; child is not null; child = child.NextSibling)
        {
            if (!context.Step())
            {
                return false;
            }

            if (child is XmlElement || (IsText(child) && child.Value?.Length > 0))
            {
                context.AddMessage("A null property cannot contain content.",
                    "Clear the value or mark it as filled in.", includeField: true);
                return false;
            }
        }

        isNull = true;
        return true;
    }

    public bool TryReadText(ValidationContext context, out string? text)
    {
        text = Text;

        if (!TryReadNull(context, out bool isNull))
        {
            return false;
        }

        if (!isNull && Element is XmlElement property)
        {
            int length = 0;

            for (XmlNode? child = property.FirstChild; child is not null; child = child.NextSibling)
            {
                if (!context.Step())
                {
                    return false;
                }

                if (child is XmlElement)
                {
                    context.Add("Text is required.", "must contain a text value.");
                    return false;
                }

                int size = child.Value?.Length ?? 0;

                if (IsText(child) && size > Limits.Characters - length)
                {
                    context.Add("Field text limit exceeded.", "is too long. Shorten the value.");
                    return false;
                }

                if (IsText(child))
                {
                    length += size;
                }
            }

            text = property.InnerText;
        }

        if (text?.Length > Limits.Characters)
        {
            context.Add("Field text limit exceeded.", "is too long. Shorten the value.");
            return false;
        }

        return true;
    }

    public static void CheckMissing(bool allowed, ValidationContext context)
    {
        if (!allowed)
        {
            context.Add("A nonblank value is required.", "is required.");
        }
    }

    public static bool IsElement(XmlElement element, string name)
    {
        return element.Name == name && element.NamespaceURI.Length == 0;
    }

    public static bool IsText(XmlNode node)
    {
        return node.NodeType is XmlNodeType.Text or XmlNodeType.CDATA or XmlNodeType.Whitespace
            or XmlNodeType.SignificantWhitespace;
    }
}
