using System;
using System.Collections.Generic;
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
        var quoted = new List<string>();

        V.AssertInput(schema, iom);
        var (input, inn) = V.ParseInput(schema, iom, value =>
        {
            quoted.Add(value);
            return Escape(value);
        });

        Assert.Equal("N'O''Brien'", (object)input.name);
        Assert.Equal("N''", (object)input.empty);
        Assert.Null((object?)input.missing);
        Assert.Collection(quoted, value => Assert.Equal("O'Brien", value),
            value => Assert.Empty(value));
        Assert.Equal(before, iom.dom.OuterXml);
        var context = Assert.IsType<Innovator>((object?)inn);
        Assert.Equal("ok", context.newResult("ok").getResult());
    }

    [Fact]
    public void QuotingHandlesDefaultsAttributesAndNestedItemsInAnAmlBatch()
    {
        var part = V.Item("Part").ID().Property("name", V.String().SqlQuoted().Default("部品"));
        var schema = V.Array(V.Item().Attribute("action", V.String().OneOf("edit").SqlQuoted())
            .Property("related_id", part));
        const string id = "783BA98DA5D04BF9AF4B1760826221F0";
        string aml = "<AML><Item action='edit'><related_id>" +
            $"<Item type='Part' id='{id}'/></related_id></Item></AML>";
        var (items, inn) = V.ParseInput(schema, aml, Escape);

        Assert.Null((object?)inn);
        Assert.Equal("N'edit'", (object)items[0]["@action"]);
        Assert.Equal("N'部品'", (object)items[0].related_id.name);
        Assert.Equal(id, (object)items[0].related_id.id);
    }

    [Fact]
    public void InvalidInputIsRejectedBeforeQuotingAnyValue()
    {
        var schema = V.Array(V.Item().Property("name", V.String().Length(1, 7).SqlQuoted()));
        const string aml = "<AML><Item><name>O'Brien</name></Item><Item><name/></Item></AML>";
        var error = Assert.Throws<ValidationException>(() => V.ParseInput(schema, aml,
            _ => throw new InvalidOperationException("Unexpected SQL escaping call")));

        Assert.Equal("$[1].name", Assert.Single(error.Issues).Path);
    }

    [Fact]
    public void QuotingRequiresAnEscapeFunctionAndAStringResult()
    {
        var schema = V.Item().Property("name", V.String().SqlQuoted());
        const string aml = "<Item><name>Part</name></Item>";

        V.AssertInput(schema, aml);
        var error = Assert.Throws<ArgumentNullException>(() => V.ParseInput(schema, aml));
        Assert.Equal("escapeSqlValue", error.ParamName);
        Assert.Throws<InvalidOperationException>(() => V.ParseInput(schema, aml, _ => null!));
        Assert.Throws<ArgumentException>(() => V.Item().ID(V.String().SqlQuoted()));
    }

    private static string Escape(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }
}
