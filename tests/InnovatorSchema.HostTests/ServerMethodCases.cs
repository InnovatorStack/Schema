using System.Collections.Generic;
using System.Linq;

namespace InnovatorSchema.HostTests;

internal static partial class ServerMethodCases
{
    public static IEnumerable<ServerMethodCase> All()
    {
        return JsonCases().Concat(AmlCases()).Concat(XmlCases());
    }

    private static ServerMethodCase Json(
        string name, string payload, params ExpectedIssue[] issues)
    {
        return new ServerMethodCase("JSON " + name, "ValidateJson", payload, issues);
    }

    private static string Order(
        string number = "\"P-100\"", string quantity = "2.5", string extra = "")
    {
        return "{\"item_number\":" + number + ",\"quantity\":" + quantity + extra + "}";
    }

    private static ServerMethodCase Aml(
        string name, string payload, params ExpectedIssue[] issues)
    {
        return new ServerMethodCase("AML " + name, "ValidateAml", payload, issues);
    }

    private static string Part(
        string properties = "", string quantity = "2.5", string related = "P-200")
    {
        return "<Item type='Part' action='add'><item_number> P-100 </item_number>" + properties +
            "<Relationships><Item type='Part BOM'><quantity>" + quantity +
            "</quantity><related_id><Item type='Part'><item_number>" + related +
            "</item_number></Item></related_id></Item></Relationships></Item>";
    }

    private static ServerMethodCase Xml(
        string name, string payload, params ExpectedIssue[] issues)
    {
        return new ServerMethodCase("XML " + name, "ValidateXml", payload, issues);
    }

    private static Dictionary<string, string> Option(string name, string value = "1")
    {
        return new Dictionary<string, string> { { name, value } };
    }
}
