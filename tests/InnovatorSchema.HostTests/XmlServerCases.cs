using System.Collections.Generic;

namespace InnovatorSchema.HostTests;

internal static partial class ServerMethodCases
{
    private static IEnumerable<ServerMethodCase> XmlCases()
    {
        const string item = "<Item><name>example</name></Item>";
        yield return Xml("literal Item element", "<doc>" + item + "</doc>") with
        {
            Output = "{\"Name\":\"example\",\"LabelState\":\"Missing\"}"
        };
        yield return Xml("text and attribute", "<doc action='check'>" + item +
            "<label>label &amp; &lt;value&gt;</label></doc>") with
        {
            Output = "{\"Label\":\"label & <value>\",\"Action\":\"check\"}"
        };
        yield return Xml("empty text", "<doc>" + item + "<label/></doc>") with
        {
            Output = "{\"Label\":\"\",\"LabelState\":\"Present\"}"
        };
        yield return Xml("literal is_null attribute", "<doc>" + item +
            "<label is_null='1'>ordinary XML</label></doc>") with
        {
            Output = "{\"Label\":\"ordinary XML\"}"
        };
        yield return Xml("literal nonboolean attribute", "<doc>" + item +
            "<label is_null='anything'>text</label></doc>");
        yield return Xml("missing nested value", "<doc><Item/></doc>",
            new ExpectedIssue("Required", "$.Item.name"));
        yield return Xml("duplicate", "<doc>" + item + "<label>a</label><label>b</label></doc>",
            new ExpectedIssue("DuplicateProperty", "$.label"));
        yield return Xml("unknown", "<doc>" + item + "<other/></doc>",
            new ExpectedIssue("UnknownProperty", "$.other"));
        yield return Xml("malformed", "<doc>", new ExpectedIssue("MalformedXml", "$"));
        yield return Xml("DTD prohibited", "<!DOCTYPE doc [<!ENTITY x 'text'>]><doc/>",
            new ExpectedIssue("MalformedXml", "$"));
    }
}
