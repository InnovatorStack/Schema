using Xunit;
using V = InnovatorSchema.Validator;

namespace InnovatorSchema.Tests;

public sealed class InputSemanticsTests
{
    [Theory]
    [InlineData("\"\\uD800\"")]
    [InlineData("\"\\uDC00\"")]
    [InlineData("\"\\uD800x\"")]
    [InlineData("\"\\uD800\\u0041\"")]
    [InlineData("{\"\\uD800\":1}")]
    [InlineData("{\"ignored\":\"\\uDC00\"}")]
    public void MalformedUnicodeIsAnInputIssue(string payload)
    {
        var schema = V.Object().UnknownProperties(UnknownPropertyHandling.Allow);
        var result = V.ValidateJson(schema, payload);

        Assert.False(result.HasOutput);
        Assert.Equal(IssueCode.MalformedJson, Assert.Single(result.Issues).Code);
    }

    [Theory]
    [InlineData('\uD800')]
    [InlineData('\uDC00')]
    public void UnpairedRawUtf16IsAnInputIssue(char character)
    {
        var result = V.ValidateJson(V.String(), "\"" + character + "\"");

        Assert.Equal(IssueCode.MalformedJson, Assert.Single(result.Issues).Code);
    }

    [Theory]
    [InlineData("\"\\uD83D\\uDE80\"", "🚀")]
    [InlineData("\"🚀\"", "🚀")]
    [InlineData("\"\\\\uD800\"", "\\uD800")]
    [InlineData("\"\\\\\\\\uD800\"", "\\\\uD800")]
    public void ValidUnicodeAndLiteralEscapesRetainTheirValue(string payload, string expected)
    {
        var result = V.ValidateJson(V.String(), payload);

        Assert.True(result.IsValid);
        Assert.Equal(expected, result.Output);
    }

    [Theory]
    [InlineData("")]
    [InlineData("true")]
    [InlineData("2")]
    public void AmlNullMarkerFailuresKeepTheSelectedFieldPath(string marker)
    {
        var schema = V.Item("Part").Property("weight", V.DecimalText().Nullable());
        var result = V.ValidateAml(schema,
            "<Item type='Part'><weight is_null='" + marker + "'/></Item>");
        var issue = Assert.Single(result.Issues);

        Assert.Equal(IssueCode.InvalidBoolean, issue.Code);
        Assert.Equal("$.weight", issue.Path.ToString());
    }

    [Fact]
    public void AmlNullWithContentFailsEvenWhenTheFieldAllowsNull()
    {
        var schema = V.Item("Part").Property("weight", V.DecimalText().Nullable());
        var result = V.ValidateAml(schema,
            "<Item type='Part'><weight is_null='1'>2</weight></Item>");
        var issue = Assert.Single(result.Issues);

        Assert.Equal(IssueCode.NullNotAllowed, issue.Code);
        Assert.Equal("$.weight", issue.Path.ToString());
    }

    [Theory]
    [InlineData("1")]
    [InlineData("anything")]
    public void GenericXmlTreatsIsNullAsAnOrdinaryAttribute(string marker)
    {
        var result = V.ValidateXml(V.String(), "<label is_null='" + marker + "'>text</label>");

        Assert.True(result.IsValid);
        Assert.Equal("text", result.Output);
    }

    [Fact]
    public void GenericXmlKeepsAnItemChildAtItsDeclaredPath()
    {
        var name = V.Field("name", V.String());
        var item = V.Field("Item", V.Object(name));
        var schema = V.Object(item);
        var valid = V.ValidateXml(schema, "<doc><Item><name>example</name></Item></doc>");
        var invalid = V.ValidateXml(schema, "<doc><Item/></doc>");

        Assert.Equal("example", valid.Output.Get(item).Get(name));
        Assert.Equal("$.Item.name", Assert.Single(invalid.Issues).Path.ToString());
    }
}
