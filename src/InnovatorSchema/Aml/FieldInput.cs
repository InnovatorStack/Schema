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
            context.Add("is_null must be 0 or 1.");
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
                context.Add("A null property cannot contain content.");
                return false;
            }
        }

        isNull = true;
        return true;
    }

    public static void CheckMissing(bool allowsNullish, ValidationContext context)
    {
        if (!allowsNullish)
        {
            context.Add("A nonblank value is required.");
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
