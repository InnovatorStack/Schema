using System.Collections.Generic;
using System.Linq;

namespace InnovatorSchema.HostTests;

internal static partial class ServerMethodCases
{
    private const string BomPath = "$.Relationships[\"Part BOM\"]";

    private static IEnumerable<ServerMethodCase> AmlCases()
    {
        yield return Aml("single Item", Part()) with
        {
            Output = "{\"ItemNumber\":\"P-100\",\"RowCount\":1,\"Quantity\":2.5," +
                "\"RelatedNumber\":\"P-200\",\"WeightState\":\"Missing\"}"
        };
        yield return Aml("single AML", "<AML>" + Part() + "</AML>");
        yield return Aml("native metadata", Part("<weight>1.5</weight><flag>1</flag>")) with
        {
            Output = "{\"WeightState\":\"Present\"}"
        };
        yield return Aml("nullable property", Part("<weight is_null='1'/>")) with
        {
            Output = "{\"WeightState\":\"Null\"}"
        };
        yield return Aml("zero marker", Part("<weight is_null='0'>1.5</weight>"));
        yield return Aml("other relationship type",
            Part().Replace("</Relationships>", "<Item type='Document'/></Relationships>",
                System.StringComparison.Ordinal));
        yield return Aml("type", Part().Replace("type='Part' action", "type='Document' action",
            System.StringComparison.Ordinal), new ExpectedIssue("ItemType", "$[@\"type\"]"));
        yield return Aml("action", Part().Replace("action='add'", "action='edit'",
            System.StringComparison.Ordinal),
            new ExpectedIssue("Custom", "$[@\"action\"]",
                "An add action is required.", "action.add"));
        yield return Aml("missing action", Part().Replace(" action='add'", "",
            System.StringComparison.Ordinal), new ExpectedIssue("Required", "$[@\"action\"]"));
        yield return Aml("missing number", Part().Replace(
            "<item_number> P-100 </item_number>", "", System.StringComparison.Ordinal),
            new ExpectedIssue("Required", "$.item_number"));
        yield return Aml("blank", Part().Replace(" P-100 ", " ", System.StringComparison.Ordinal),
            new ExpectedIssue("NotBlank", "$.item_number", "Enter an item number."));
        yield return Aml("quantity minimum", Part(quantity: "0"),
            new ExpectedIssue("Minimum", BomPath + "[0].quantity",
                "Quantity must be at least 0.001."));
        yield return Aml("nested missing number", Part(related: ""),
            new ExpectedIssue("NotBlank", BomPath + "[0].related_id.item_number",
                "Enter an item number."));

        foreach (ServerMethodCase test in AmlFailures())
        {
            yield return test;
        }

        foreach (ServerMethodCase test in AmlMarkersAndBooleans())
        {
            yield return test;
        }

        foreach (ServerMethodCase test in IomCases())
        {
            yield return test;
        }
    }

    private static IEnumerable<ServerMethodCase> AmlFailures()
    {
        yield return Aml("no rows", "<Item type='Part' action='add'><item_number>P-100" +
            "</item_number></Item>", new ExpectedIssue("MinimumCount", BomPath));
        string row = "<Item type='Part BOM'><quantity>1</quantity><related_id><Item type='Part'>" +
            "<item_number>P-200</item_number></Item></related_id></Item>";
        yield return Aml("too many rows", "<Item type='Part' action='add'><item_number>P-100" +
            "</item_number><Relationships>" + string.Concat(Enumerable.Repeat(row, 101)) +
            "</Relationships></Item>",
            new ExpectedIssue("Custom", BomPath, "Use at most 100 BOM rows.", "bom.limit"));
        yield return Aml("ID requires expansion", Part().Replace(
            "<related_id><Item type='Part'><item_number>P-200</item_number></Item></related_id>",
            "<related_id>0123456789ABCDEF0123456789ABCDEF</related_id>",
            System.StringComparison.Ordinal),
            new ExpectedIssue("ExpansionRequired", BomPath + "[0].related_id"));
        yield return Aml("empty AML", "<AML/>", new ExpectedIssue("Cardinality", "$"));
        yield return Aml("multiple roots", "<AML>" + Part() + Part() + "</AML>",
            new ExpectedIssue("Cardinality", "$"));
        yield return Aml("wrong root", "<doc/>", new ExpectedIssue("TypeMismatch", "$"));
        yield return Aml("malformed", "<Item", new ExpectedIssue("MalformedXml", "$"));
        yield return Aml("DTD prohibited", "<!DOCTYPE Item [<!ENTITY x 'P-100'>]>" + Part(),
            new ExpectedIssue("MalformedXml", "$"));
        yield return Aml("namespace", "<Item xmlns='urn:test' type='Part'/>",
            new ExpectedIssue("TypeMismatch", "$"));
        yield return Aml("duplicate property", Part("<item_number>P-200</item_number>"),
            new ExpectedIssue("DuplicateProperty", "$.item_number"));

        foreach (string value in new[] { "1,5", " 1", "NaN", "1.00000000000000000000000000001" })
        {
            yield return Aml("invalid decimal " + value, Part(quantity: value),
                new ExpectedIssue("InvalidNumber", BomPath + "[0].quantity"));
        }

        yield return Aml("Aras error", Part(quantity: "0")) with
        {
            Error = BomPath + "[0].quantity: Quantity must be at least 0.001."
        };
    }

    private static IEnumerable<ServerMethodCase> AmlMarkersAndBooleans()
    {
        foreach (string value in new[] { "0", "1" })
        {
            yield return Aml("boolean " + value, Part("<flag>" + value + "</flag>"));
        }

        foreach (string value in new[] { "true", "false", "True", " 1", "2", "" })
        {
            yield return Aml("invalid boolean " + value, Part("<flag>" + value + "</flag>"),
                new ExpectedIssue("InvalidBoolean", "$.flag"));
        }

        foreach (string marker in new[] { "", "true", "2" })
        {
            yield return Aml("invalid null marker " + marker,
                Part("<weight is_null='" + marker + "'/>"),
                new ExpectedIssue("InvalidBoolean", "$.weight"));
        }

        yield return Aml("null plus content", Part("<weight is_null='1'>2</weight>"),
            new ExpectedIssue("NullNotAllowed", "$.weight"));
        yield return Aml("nonnullable property", Part("<flag is_null='1'/>"),
            new ExpectedIssue("NullNotAllowed", "$.flag"));
    }

    private static IEnumerable<ServerMethodCase> IomCases()
    {
        yield return Aml("IOM valid", Part()) with { Options = Option("input", "iom") };
        yield return Aml("IOM nullable", Part("<weight is_null='1'/>")) with
        {
            Options = Option("input", "iom"),
            Output = "{\"WeightState\":\"Null\"}"
        };
        yield return Aml("IOM invalid row", Part(quantity: "0"),
            new ExpectedIssue("Minimum", BomPath + "[0].quantity",
                "Quantity must be at least 0.001.")) with
        {
            Options = Option("input", "iom")
        };
        yield return Aml("IOM Aras error", Part(quantity: "0")) with
        {
            Options = Option("input", "iom"),
            Error = "Quantity must be at least 0.001."
        };
    }
}
