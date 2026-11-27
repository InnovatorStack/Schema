using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace InnovatorSchema;

internal static class InputAssertion
{
    public static void Assert(ItemSchema item, ItemArraySchema? array, object? input)
    {
        Read(item, array, input, results: null);
    }

    public static (ParsedItem Input, object? Innovator) Parse(ItemSchema item, object? input,
        Func<string, string>? escapeSqlValue)
    {
        var results = new List<ParsedItem>(1);
        IomItem? iom = Read(item, null, input, results);
        QuoteSqlValues(item, results, escapeSqlValue);
        return results.Count == 1 ? (results[0], iom?.Innovator) :
            throw new InvalidOperationException("The validated input did not produce an Item.");
    }

    public static (IReadOnlyList<ParsedItem> Input, object? Innovator) Parse(
        ItemArraySchema array, object? input, Func<string, string>? escapeSqlValue)
    {
        var results = new List<ParsedItem>();
        IomItem? iom = Read(array.Item, array, input, results);
        QuoteSqlValues(array.Item, results, escapeSqlValue);
        return (results.AsReadOnly(), iom?.Innovator);
    }

    private static void QuoteSqlValues(ItemSchema item, List<ParsedItem> results,
        Func<string, string>? escapeSqlValue)
    {
        if (!item.QuotesSql)
        {
            return;
        }

        if (escapeSqlValue is null)
        {
            throw new ArgumentNullException(nameof(escapeSqlValue),
                "Supply a SQL escaping function when the schema uses SqlQuoted().");
        }

        foreach (ParsedItem result in results)
        {
            result.QuoteSqlValues(escapeSqlValue);
        }
    }

    private static IomItem? Read(ItemSchema item, ItemArraySchema? array, object? input,
        List<ParsedItem>? results)
    {
        var context = new ValidationContext();
        IomItem? iom = null;

        if (input is string aml)
        {
            ReadAml(item, array, aml, context, results);
        }
        else if (input is null)
        {
            context.AddMessage("An AML document or IOM Item is required.",
                "Provide the items to check.");
        }
        else
        {
            var source = IomItem.Read(input);
            ReadIom(item, array, source, context, results);
            iom = source;
        }

        context.ThrowIfInvalid();
        return iom;
    }

    private static void ReadAml(ItemSchema item, ItemArraySchema? array, string aml,
        ValidationContext context, List<ParsedItem>? results)
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
                ReadRow(item, array, root, null, 0, context, results);
            }

            return;
        }

        if (!FieldInput.IsElement(root, "AML"))
        {
            context.AddMessage("AML must have an Item or AML root.",
                "Send the request as an item or a list of items.");
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

                ReadRow(item, array, element, null, index++, context, results);
            }
        }
    }

    private static XmlElement? Load(string aml, ValidationContext context)
    {
        if (aml.Length > Limits.Characters)
        {
            context.AddMessage("AML length limit exceeded.",
                "The request is too large. Reduce the amount of data and try again.");
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
            context.AddMessage("Malformed AML or XML limits exceeded.",
                "The request could not be read. Check its format and size.");
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
            context.AddMessage("An AML collection cannot be null.", "Provide an item list.");
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
                context.AddMessage("An AML collection can only contain Items.",
                    "The list must contain items only.");
                return -1;
            }
        }

        return count;
    }

    private static void ReadIom(ItemSchema item, ItemArraySchema? array, IomItem input,
        ValidationContext context, List<ParsedItem>? results)
    {
        int count = input.Count;

        if (count < 0)
        {
            context.AddMessage("An IOM error or result without Items cannot be validated.",
                "Provide items from a successful request.");
            return;
        }

        if (!CheckCount(count, array, context))
        {
            return;
        }

        for (int index = 0; index < count && context.Step(); index++)
        {
            IomItem row = input.At(index);
            ReadRow(item, array, row.Element, row, index, context, results);
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
            context.AddMessage("Item collection limit exceeded.",
                "Split the items into smaller batches and try again.");
            return false;
        }

        if (array is not null)
        {
            array.ValidateCount(count, context);
            return true;
        }

        if (count != 1)
        {
            context.AddMessage("Exactly one Item is required.", "Provide exactly one item.");
            return false;
        }

        return true;
    }

    private static void ReadRow(ItemSchema item, ItemArraySchema? array, XmlElement? node,
        IomItem? iom, int index, ValidationContext context, List<ParsedItem>? results)
    {
        using var scope = array is null ? default(ValidationContext.Scope) : context.Enter(index);

        if (node is null || !FieldInput.IsElement(node, "Item"))
        {
            context.AddMessage("An Item is required.", "Provide an item.");
            return;
        }

        var source = new FieldInput(null, node, iom);

        if (!source.TryReadNull(context, out bool isNull))
        {
            return;
        }

        if (isNull)
        {
            context.AddMessage("An Item cannot be null.", "Provide an item for this entry.");
            return;
        }

        var parsed = item.ReadItem(node, iom, context, capture: results is not null);

        if (parsed is not null)
        {
            results!.Add(parsed);
        }
    }
}
