using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace InnovatorSchema.HostTests;

internal sealed record AssemblyCase(string Name, string Input, decimal? Weight, string[] Paths);

internal static class AssemblyCases
{
    private const string Rows = "$.Relationships[\"Part BOM\"]";
    private const string Part = Rows + "[0].related_id";
    private const string Drawings = Part + ".Relationships[\"Part Document\"]";
    private const string Document = Drawings + "[0].related_id";
    private const string FirstRow = "Relationships/Item[@type='Part BOM'][1]";
    private const string Component = FirstRow + "/related_id/Item";
    private const string Documents = Component + "/Relationships";
    private const string Drawing = Documents + "/Item[@type='Part Document'][1]/related_id/Item";

    public static IEnumerable<AssemblyCase> Create(string input)
    {
        yield return new AssemblyCase("expanded assembly", input, 4m, Array.Empty<string>());
        yield return Valid(input, "missing weight", root =>
            At(root, Component + "/weight").Remove(), 1.5m);
        yield return Valid(input, "null weight", root => Null(At(root, Component + "/weight")),
            1.5m);
        yield return Valid(input, "missing flag", root =>
            At(root, Component + "/is_released").Remove(), 4m);
        yield return Valid(input, "missing description", root => At(root, "description").Remove(),
            4m);
        yield return Valid(input, "minimum quantity", root =>
            At(root, FirstRow + "/quantity").Value = "0.001", 1.50125m);
        yield return Valid(input, "maximum BOM count", root =>
            Repeat(root, "Relationships", "Part BOM", 100), 250m);
        yield return Valid(input, "maximum document count", root =>
            Repeat(root, Documents, "Part Document", 5), 4m);

        foreach (var failure in Failures(input))
        {
            yield return failure;
        }

        yield return Invalid(input, "independent failures", BreakSeveralFields,
            "$.item_number", "$.name", Rows + "[0].quantity", Rows + "[0].sort_order",
            Part + ".item_number", Part + ".weight", Part + ".is_released",
            Document + ".item_number", Document + ".major_rev", Rows + "[1].quantity");
    }

    private static IEnumerable<AssemblyCase> Failures(string input)
    {
        yield return Invalid(input, "wrong root type", root =>
            root.SetAttributeValue("type", "Document"), "$[@\"type\"]");
        yield return Invalid(input, "wrong component type", root =>
            At(root, Component).SetAttributeValue("type", "Document"), Part + "[@\"type\"]");
        yield return Invalid(input, "unexpanded component", root =>
            At(root, FirstRow + "/related_id").Value = new string('A', 32), Part);
        yield return Invalid(input, "missing documents", root =>
            At(root, Documents).Remove(), Drawings);
        yield return Invalid(input, "blank document number", root =>
            At(root, Drawing + "/item_number").Value = " ", Document + ".item_number");
        yield return Invalid(input, "invalid revision", root =>
            At(root, Drawing + "/major_rev").Value = "bad", Document + ".major_rev");
        yield return Invalid(input, "negative quantity", root =>
            At(root, FirstRow + "/quantity").Value = "-1", Rows + "[0].quantity");
        yield return Invalid(input, "fractional sequence", root =>
            At(root, FirstRow + "/sort_order").Value = "1.5", Rows + "[0].sort_order");
        yield return Invalid(input, "decimal overflow", root =>
            At(root, Component + "/weight").Value = "1e100", Part + ".weight");
        yield return Invalid(input, "invalid flag", root =>
            At(root, Component + "/is_released").Value = "true", Part + ".is_released");
        yield return Invalid(input, "null expanded document", root =>
            Null(At(root, Drawing + "/..")), Document);
        yield return Invalid(input, "empty BOM", root =>
            Repeat(root, "Relationships", "Part BOM", 0), Rows);
        yield return Invalid(input, "too many BOM rows", root =>
            Repeat(root, "Relationships", "Part BOM", 101), Rows);
        yield return Invalid(input, "too many documents", root =>
            Repeat(root, Documents, "Part Document", 6), Drawings);
        yield return Invalid(input, "duplicate property", root =>
            root.Add(new XElement("item_number", "P-400")), "$.item_number");
    }

    private static void BreakSeveralFields(XElement root)
    {
        At(root, "item_number").Value = " ";
        At(root, "name").Value = "";
        At(root, FirstRow + "/quantity").Value = "0";
        At(root, FirstRow + "/sort_order").Value = "0";
        At(root, Component + "/item_number").Value = " ";
        At(root, Component + "/weight").Value = "-1";
        At(root, Component + "/is_released").Value = "true";
        At(root, Drawing + "/item_number").Value = " ";
        At(root, Drawing + "/major_rev").Value = "bad";
        At(root, "Relationships/Item[@type='Part BOM'][2]/quantity").Value = "bad";
    }

    private static AssemblyCase Valid(string input, string name, Action<XElement> edit,
        decimal weight)
    {
        return new AssemblyCase(name, Edit(input, edit), weight, Array.Empty<string>());
    }

    private static AssemblyCase Invalid(string input, string name, Action<XElement> edit,
        params string[] paths)
    {
        return new AssemblyCase(name, Edit(input, edit), null, paths);
    }

    private static string Edit(string input, Action<XElement> change)
    {
        var root = XElement.Parse(input);
        change(root);
        return root.ToString(SaveOptions.DisableFormatting);
    }

    private static XElement At(XElement root, string path)
    {
        return root.XPathSelectElement(path) ??
            throw new InvalidOperationException("An assembly fixture node is missing: " + path);
    }

    private static void Null(XElement property)
    {
        property.RemoveNodes();
        property.SetAttributeValue("is_null", "1");
    }

    private static void Repeat(XElement root, string path, string itemType, int count)
    {
        var container = At(root, path);
        var items = container.Elements("Item")
            .Where(item => (string?)item.Attribute("type") == itemType).ToArray();
        var sample = new XElement(items[0]);

        foreach (var item in items)
        {
            item.Remove();
        }

        for (int index = 0; index < count; index++)
        {
            container.Add(new XElement(sample));
        }
    }
}
