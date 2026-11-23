using System.IO;
using System.Xml;

namespace InnovatorSchema;

internal static class InputAssertion
{
    public static void Assert(ItemSchema item, ItemArraySchema? array, object? input)
    {
        var context = new ValidationContext();

        if (input is string aml)
        {
            AssertAml(item, array, aml, context);
        }
        else if (input is null)
        {
            context.Add("An AML document or IOM Item is required.");
        }
        else
        {
            AssertIom(item, array, IomItem.Read(input), context);
        }

        context.ThrowIfInvalid();
    }

    private static void AssertAml(
        ItemSchema item, ItemArraySchema? array, string aml, ValidationContext context)
    {
        XmlElement? root = Load(aml, context);

        if (root is null)
        {
            return;
        }

        if (FieldInput.IsElement(root, "Item"))
        {
            if (CheckCount(1, array, context))
            {
                AssertRow(item, array, root, null, 0, context);
            }

            return;
        }

        if (!FieldInput.IsElement(root, "AML"))
        {
            context.Add("AML must have an Item or AML root.");
            return;
        }

        int count = Count(root, context);

        if (!CheckCount(count, array, context))
        {
            return;
        }

        int index = 0;

        for (XmlNode? child = root.FirstChild; child is not null; child = child.NextSibling)
        {
            if (child is XmlElement element)
            {
                if (!context.Step())
                {
                    return;
                }

                AssertRow(item, array, element, null, index++, context);
            }
        }
    }

    private static XmlElement? Load(string aml, ValidationContext context)
    {
        if (aml.Length > Limits.Characters)
        {
            context.Add("AML length limit exceeded.");
            return null;
        }

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            MaxCharactersInDocument = Limits.Characters,
            XmlResolver = null
        };
        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };

        try
        {
            using var source = new StringReader(aml);
            using var reader = new BoundedXmlReader(XmlReader.Create(source, settings));
            document.Load(reader);
            return document.DocumentElement;
        }
        catch (XmlException)
        {
            context.Add("Malformed AML or XML limits exceeded.");
            return null;
        }
    }

    private static int Count(XmlElement root, ValidationContext context)
    {
        var input = new FieldInput(null, root, null);

        if (!input.TryReadNull(context, out bool isNull))
        {
            return -1;
        }

        if (isNull)
        {
            context.Add("An AML collection cannot be null.");
            return -1;
        }

        int count = 0;

        for (XmlNode? child = root.FirstChild; child is not null; child = child.NextSibling)
        {
            if (child is XmlElement)
            {
                count++;
            }
            else if (FieldInput.IsText(child) && !string.IsNullOrWhiteSpace(child.Value))
            {
                context.Add("An AML collection can only contain Items.");
                return -1;
            }
        }

        return count;
    }

    private static void AssertIom(
        ItemSchema item, ItemArraySchema? array, IomItem input, ValidationContext context)
    {
        int count = input.Count;

        if (count < 0)
        {
            context.Add("An IOM error or result without Items cannot be validated.");
            return;
        }

        if (!CheckCount(count, array, context))
        {
            return;
        }

        for (int index = 0; index < count && context.Step(); index++)
        {
            IomItem row = input.At(index);
            AssertRow(item, array, row.Element, row, index, context);
        }
    }

    private static bool CheckCount(
        int count, ItemArraySchema? array, ValidationContext context)
    {
        if (count < 0)
        {
            return false;
        }

        if (count > Limits.Items)
        {
            context.Add("Item collection limit exceeded.");
            return false;
        }

        if (array is not null)
        {
            array.ValidateCount(count, context);
            return true;
        }

        if (count != 1)
        {
            context.Add("Exactly one Item is required.");
            return false;
        }

        return true;
    }

    private static void AssertRow(ItemSchema item, ItemArraySchema? array, XmlElement? node,
        IomItem? iom, int index, ValidationContext context)
    {
        using var scope = array is null ? default(ValidationContext.Scope) : context.Enter(index);

        if (node is null || !FieldInput.IsElement(node, "Item"))
        {
            context.Add("An Item is required.");
            return;
        }

        var source = new FieldInput(null, node, iom);

        if (source.TryReadNull(context, out bool isNull))
        {
            if (isNull)
            {
                context.Add("An Item cannot be null.");
            }
            else
            {
                item.ValidateItem(node, iom, context);
            }
        }
    }
}
