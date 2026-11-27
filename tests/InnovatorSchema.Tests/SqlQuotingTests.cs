using System;
using Aras.IOM;
using Xunit;
using V = InnovatorSchema.Validator;

namespace InnovatorSchema.Tests;

public sealed class SqlQuotingTests
{
    [Fact]
    public void QuotingUsesTheValidatedTextAndReturnsTheIomContext()
    {
        var schema = V.Item().Property("name", V.String().SqlQuoted().Length(7))
            .Property("empty", V.String().Optional().SqlQuoted())
            .Property("missing", V.String().SqlQuoted().Optional());
        var iom = ArasIntegrationTests.ItemFrom("<Item><name>O'Brien</name><empty/></Item>");
        string before = iom.dom.OuterXml;
        V.AssertInput(schema, iom);
        var (input, inn) = V.ParseInput(schema, iom);

        Assert.Equal("'O''Brien'", (object)input.name);
        Assert.Equal("''", (object)input.empty);
        Assert.Null((object?)input.missing);
        Assert.Equal(before, iom.dom.OuterXml);
        var context = Assert.IsType<Innovator>((object?)inn);
        Assert.Equal("ok", context.newResult("ok").getResult());
    }

    [Fact]
    public void QuotingHandlesDefaultsAttributesAndNestedItemsInAnAmlBatch()
    {
        var part = V.Item("Part").ID().Property("name", V.String().SqlQuoted().Default("部品"));
        var schema = V.Items(V.Item().Attribute("action", V.String().OneOf("edit").SqlQuoted())
            .Property("related_id", part));
        const string id = "783BA98DA5D04BF9AF4B1760826221F0";
        string aml = "<AML><Item action='edit'><related_id>" +
            $"<Item type='Part' id='{id}'/></related_id></Item></AML>";
        var (items, inn) = V.ParseInput(schema, aml);

        Assert.Null((object?)inn);
        Assert.Equal("'edit'", (object)items[0]["@action"]);
        Assert.Equal("'部品'", (object)items[0].related_id.name);
        Assert.Equal(id, (object)items[0].related_id.id);
    }

    [Fact]
    public void QuotingDoesNotChangeValidationErrors()
    {
        var schema = V.Items(V.Item().Property("name", V.String().Length(1, 7).SqlQuoted()));
        const string aml = "<AML><Item><name>O'Brien</name></Item><Item><name/></Item></AML>";
        var asserted = Assert.Throws<ValidationException>(() => V.AssertInput(schema, aml));
        var parsed = Assert.Throws<ValidationException>(() => V.ParseInput(schema, aml));

        Assert.Equal("$[1].name", Assert.Single(parsed.Issues).Path);
        Assert.Equal(asserted.GetErrorString(), parsed.GetErrorString());
    }

    [Theory]
    [InlineData("ABC", "ABC")]
    [InlineData("O&apos;Brien", "O''Brien")]
    [InlineData("&apos;&apos;", "''''")]
    [InlineData("部品", "部品")]
    [InlineData("", "")]
    public void FalseEscapesOnlyApostrophes(string text, string expected)
    {
        var schema = V.Item().Property("name", V.String().Optional().SqlQuoted()
            .SqlQuoted(false));
        var (input, _) = V.ParseInput(schema, "<Item><name>" + text + "</name></Item>");
        Assert.Equal(expected, (object)input.name);
    }

    [Fact]
    public void IdRuleRejectsATransform()
    {
        Assert.Throws<ArgumentException>(() => V.Item().ID(V.Guid().SqlQuoted()));
    }
}
