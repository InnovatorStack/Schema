using System;
using System.Collections.Generic;
using Microsoft.CSharp.RuntimeBinder;
using Xunit;
using V = InnovatorSchema.Validator;

namespace InnovatorSchema.Tests;

public sealed class ParsingTests
{
    [Fact]
    public void ParseReadsDeclaredFieldsUsingTheirSchemaTypes()
    {
        const string id = "783BA98DA5D04BF9AF4B1760826221F0";
        var schema = V.Item("Part").ID(V.Guid())
            .Attribute("action", V.String().OneOf("edit"))
            .Property("action", V.String())
            .Property("weight", V.Integer().Min(0))
            .Property("cost", V.Double())
            .Property("effective_date", V.Date())
            .Property("related_id", V.Item("Part").ID());
        string aml = $"<AML><Item type='Part' id='{id}' action='edit'><action>Review</action>" +
            "<weight>4</weight><cost>1.5</cost><effective_date>2026-11-26</effective_date>" +
            "<related_id><Item type='Part' id='938599DAAAE34871BFA51167C242DB12'/>" +
            "</related_id><extra>x</extra></Item></AML>";

        var (part, _) = V.ParseInput(schema, aml);
        object partId = part.id;
        object attribute = part["@action"];
        object property = part.action;
        object indexedWeight = part["weight"];
        long weight = part.weight;
        object cost = part.cost;
        object effectiveDate = part.effective_date;
        string childId = part.related_id.id;

        Assert.Equal(Guid.ParseExact(id, "N"), Assert.IsType<Guid>(partId));
        Assert.Equal("edit", Assert.IsType<string>(attribute));
        Assert.Equal("Review", Assert.IsType<string>(property));
        Assert.Equal(4L, Assert.IsType<long>(indexedWeight));
        Assert.Equal(4L, weight);
        Assert.Equal(1.5, Assert.IsType<double>(cost));
        Assert.Equal(new DateTime(2026, 11, 26), Assert.IsType<DateTime>(effectiveDate));
        Assert.Equal("938599DAAAE34871BFA51167C242DB12", childId);
        Assert.Throws<KeyNotFoundException>(() => part["extra"]);
        Assert.Throws<RuntimeBinderException>(() => part.extra);
    }

    [Theory]
    [InlineData("")]
    [InlineData("<weight is_null='1'/>")]
    public void OptionalFieldsReturnNullForAbsentOrExplicitNullValues(string property)
    {
        var schema = V.Item().Property("weight", V.Integer().Optional())
            .Property("reference", V.String().Optional())
            .Property("related_id", V.Item("Part").Optional());
        var (part, _) = V.ParseInput(schema, "<Item>" + property + "</Item>");
        long? weight = part.weight;
        string? reference = part["reference"];
        object? related = part.related_id;

        Assert.Null(weight);
        Assert.Null(reference);
        Assert.Null(related);
    }

    [Fact]
    public void InvalidInputHasTheSameErrorsInBothEntryPoints()
    {
        var schema = V.Item("Part").ID(V.Guid())
            .Property("weight", V.Integer().Min(3));
        const string aml = "<Item type='Part' id='wrong'><weight>2</weight></Item>";

        var asserted = Assert.Throws<ValidationException>(() => V.AssertInput(schema, aml));
        var parsed = Assert.Throws<ValidationException>(() => V.ParseInput(schema, aml));

        Assert.Equal(2, parsed.Issues.Count);
        Assert.Equal(asserted.Message, parsed.Message);
        Assert.Equal(asserted.GetErrorString(), parsed.GetErrorString());
    }

    [Theory]
    [InlineData("<AML/>")]
    [InlineData("<AML><Item/><Item/></AML>")]
    public void ParseRequiresExactlyOneItem(string aml)
    {
        var error = Assert.Throws<ValidationException>(() => V.ParseInput(V.Item(), aml));
        Assert.Equal("Provide exactly one item.", error.Message);
    }

    [Fact]
    public void BatchRowsSupportTheSameAccessAndKeepTheirOrder()
    {
        var schema = V.Array(V.Item().Property("weight", V.Integer())).Min(1).Max(2);
        const string aml = "<AML><Item><weight>3</weight></Item>" +
            "<Item><weight>5</weight></Item></AML>";
        var (parts, _) = V.ParseInput(schema, aml);
        long first = parts[0].weight;
        long second = parts[1]["weight"];

        Assert.Equal(2, parts.Count);
        Assert.Equal(3L, first);
        Assert.Equal(5L, second);
        Assert.Empty(V.ParseInput(V.Array(V.Item()), "<AML/>").Input);
    }

    [Fact]
    public void InvalidBatchKeepsTheRowInTheErrorPath()
    {
        var schema = V.Array(V.Item().Property("weight", V.Integer()));
        const string aml = "<AML><Item><weight>3</weight></Item>" +
            "<Item><weight>bad</weight></Item></AML>";

        var error = Assert.Throws<ValidationException>(() => V.ParseInput(schema, aml));

        Assert.Equal("$[1].weight", Assert.Single(error.Issues).Path);
    }

    [Fact]
    public void IndexingHandlesPunctuationAndExplicitFieldsWithTheSameIdName()
    {
        const string id = "783BA98DA5D04BF9AF4B1760826221F0";
        var schema = V.Item().ID(V.Guid()).Attribute("id", V.String())
            .Property("id", V.String()).Property("part-number", V.String());
        string aml = $"<Item id='{id}'><id>reference</id><part-number>P-100</part-number></Item>";

        var (part, _) = V.ParseInput(schema, aml);
        string attribute = part["@id"];
        string property = part.id;
        string number = part["part-number"];

        Assert.Equal(id, attribute);
        Assert.Equal("reference", property);
        Assert.Equal("P-100", number);
    }

    [Theory]
    [InlineData("<Item")]
    [InlineData("<AML>text<Item/></AML>")]
    [InlineData("<!DOCTYPE Item [<!ENTITY text 'value'>]><Item>&text;</Item>")]
    public void InvalidAmlCannotReturnParsedValues(string aml)
    {
        var schema = V.Array(V.Item());
        var asserted = Assert.Throws<ValidationException>(() => V.AssertInput(schema, aml));
        var parsed = Assert.Throws<ValidationException>(() => V.ParseInput(schema, aml));
        Assert.Equal(asserted.GetErrorString(), parsed.GetErrorString());
    }

    [Fact]
    public void NestedAndBatchErrorsAreCollectedTogether()
    {
        var part = V.Item("Part").ID(V.Guid()).Property("weight", V.Double().Min(0.0));
        var schema = V.Array(V.Item().Property("related_id", part)).Max(1);
        const string aml = "<AML><Item><related_id>id-only</related_id></Item>" +
            "<Item><related_id><Item type='Part' id='wrong'><weight>-1</weight></Item>" +
            "</related_id></Item></AML>";
        var error = Assert.Throws<ValidationException>(() => V.ParseInput(schema, aml));

        Assert.Collection(error.Issues,
            issue => Assert.Equal("$", issue.Path),
            issue => Assert.Equal("$[0].related_id", issue.Path),
            issue => Assert.Equal("$[1].related_id.@id", issue.Path),
            issue => Assert.Equal("$[1].related_id.weight", issue.Path));
    }

    [Theory]
    [InlineData(long.MinValue)]
    [InlineData(0L)]
    [InlineData(long.MaxValue)]
    public void IntegerValuesRetainTheirFullPrecision(long expected)
    {
        string text = expected.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var schema = V.Item().Property("quantity", V.Integer());
        var (part, _) = V.ParseInput(schema, "<Item><quantity>" + text + "</quantity></Item>");
        object quantity = part["quantity"];

        Assert.Equal(expected, Assert.IsType<long>(quantity));
    }
}
