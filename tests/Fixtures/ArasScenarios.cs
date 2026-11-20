#if ARAS_TESTS
using System;
using System.Diagnostics.CodeAnalysis;
using Aras.IOM;
using V = InnovatorSchema.Validator;

namespace InnovatorSchema.Testing;

internal static class ArasScenarios
{
    public static void NestedItemOutput()
    {
        var innovator = OfflineConnection.CreateInnovator();
        var code = V.Field("item_number", V.String().Transform(text => text.Trim()));
        var related = V.Field("related_id", V.Item("Part", code));
        var quantity = V.Field("quantity", V.DecimalText().Min(1m));
        var bom = V.Relationships("Part BOM", V.Array(V.Item("Part BOM", related, quantity))
            .MinCount(1));
        var action = V.Attribute("action", V.String().Check(
            text => text == "add", "add-only", "An add action is required."));
        var schema = V.Item("Part", code, action, bom);
        var item = Load(innovator, "<Item type='Part' action='add'>" +
            "<item_number> P-100 </item_number><Relationships><Item type='Part BOM'>" +
            "<quantity>2.5</quantity><related_id><Item type='Part'>" +
            "<item_number>P-200</item_number></Item></related_id>" +
            "</Item></Relationships></Item>");
        string before = item.ToString();

        var result = V.Validate(schema, item);

        Require(result.IsValid, result.GetErrorString());
        Require(result.Output.Get(code) == "P-100", "Transformed root output differs.");
        var line = result.Output.Get(bom)[0].Value;
        Require(line.Get(quantity) == 2.5m, "Text decimal did not become typed output.");
        Require(line.Get(related).Get(code) == "P-200", "Expanded Item output differs.");
        Require(result.Output.Get(action) == "add", "Attribute output differs.");
        Require(item.ToString() == before, "Validation changed the source Item.");
    }

    public static void RelationshipFailureAndErrors()
    {
        var innovator = OfflineConnection.CreateInnovator();
        var line = V.Item("Part BOM").Property("quantity", V.DecimalText().Min(1m));
        var schema = V.Item("Part", V.Relationships("Part BOM", V.Array(line)));
        var item = Load(innovator, "<Item type='Part'><Relationships>" +
            "<Item type='Other'/><Item type='Part BOM'><quantity>0</quantity></Item>" +
            "</Relationships></Item>");
        string before = item.ToString();
        var result = V.Validate(schema, item);

        Require(!result.IsValid && !result.HasOutput, "Invalid Item produced output.");
        Require(result.Issues.Count == 1, "Unexpected relationship issue count.");
        Require(result.Issues[0].Path.ToString() ==
            "$.Relationships[\"Part BOM\"][0].quantity", "Relationship path differs.");
        Require(result.Error is not null, "Invalid input has no structured error.");
        Require(item.ToString() == before, "Validation changed the source Item.");

        var text = V.String().Check(value => false, "example", "Invalid <item> & \"value\".");
        var json = V.ValidateJson(text, "\"input\"");
        Require(json.GetErrorString().Contains("Invalid <item> & \"value\".",
            StringComparison.Ordinal), "The plain error changed XML-sensitive text.");
    }

    public static void CardinalityAndExpansion()
    {
        var innovator = OfflineConnection.CreateInnovator();
        var schema = V.Item("Part");
        var many = Load(innovator, "<AML><Item type='Part'/><Item type='Part'/></AML>");
        Require(V.Validate(schema, many).Issues[0].Code == IssueCode.Cardinality,
            "Multiple Items did not fail single-Item cardinality.");
        Require(V.Validate(schema, innovator.newError("Existing error")).Issues[0].Code ==
            IssueCode.Cardinality, "An existing error was accepted as input.");
        Require(V.ValidateAml(schema, "<AML><Item type='Part'/></AML>").IsValid,
            "A singleton AML envelope was rejected.");
        Require(V.ValidateAml(schema, "<AML/>").Issues[0].Code == IssueCode.Cardinality,
            "An empty AML envelope was accepted.");
        Require(!V.ValidateAml(schema, "<Other type='Part'/>").IsValid,
            "A non-Item root was accepted.");
        var nested = V.Item("Part").Property("related_id", V.Item("Part"));
        var idOnly = V.ValidateAml(nested,
            "<Item type='Part'><related_id>ABC123</related_id></Item>");
        Require(idOnly.Issues[0].Code == IssueCode.ExpansionRequired,
            "An unexpanded reference did not fail without fetching.");
        Require(idOnly.Issues[0].Path.ToString() == "$.related_id", "Reference path differs.");
        Require(!V.ValidateAml(nested,
            "<Item type='Part'><related_id type='Part'/></Item>").IsValid,
            "An attribute on an ordinary element impersonated an expanded Item.");
        Require(!V.ValidateAml(nested, "<Item type='Part'><related_id>text" +
            "<Item type='Part'/></related_id></Item>").IsValid,
            "An expanded Item concealed mixed text in its property wrapper.");
        Require(!V.ValidateAml(schema, "<Item xmlns='urn:other' type='Part'/>").IsValid,
            "A namespaced element impersonated an Aras Item.");
    }

    public static void ReusableSchemas()
    {
        var first = OfflineConnection.CreateInnovator();
        var second = OfflineConnection.CreateInnovator();
        var schema = CountingSchema.Instance;
        int before = CountingSchema.Constructions;
        var firstItem = first.newItem("Part");
        var secondItem = second.newItem("Part");
        firstItem.setProperty("code", "P-100");
        secondItem.setProperty("code", "P-200");
        var firstResult = V.Parse(schema, firstItem);
        var secondResult = V.Parse(schema, secondItem);

        Require(firstResult is not null && secondResult is not null, "Parsed data is missing.");
        Require(CountingSchema.Constructions == before, "Validation reconstructed the schema.");
        Require(firstResult.code == "P-100", "The first parsed value changed.");
        Require(secondResult.code == "P-200", "The second parsed value changed.");
        Require(!ReferenceEquals(firstResult, secondResult), "Per-call data is shared.");
    }

    private static Item Load(Innovator innovator, string aml)
    {
        var item = innovator.newItem();
        item.loadAML(aml);

        return item;
    }

    private static void Require([DoesNotReturnIf(false)] bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public sealed class CountingSchema : Schema<ValidatedObject>
    {
        public static CountingSchema Instance { get; } = new CountingSchema();

        public CountingSchema() : base(V.Object().Property("code", V.String()))
        {
            Constructions++;
        }

        public static int Constructions { get; private set; }
    }
}
#endif
