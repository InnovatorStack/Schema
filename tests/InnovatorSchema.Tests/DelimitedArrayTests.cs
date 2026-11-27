using System;
using System.Linq;
using Aras.IOM;
using Xunit;
using V = InnovatorSchema.Validator;

namespace InnovatorSchema.Tests;

public sealed class DelimitedArrayTests
{
    private const string FirstId = "783BA98DA5D04BF9AF4B1760826221F0";
    private const string SecondId = "938599daaae34871bfa51167c242db12";

    [Fact]
    public void TheMethodSchemaReturnsEachQuotedIdAsAStringArrayElement()
    {
        var schema = V.Item().ID()
            .Attribute("idList", V.Array(V.Guid().SqlQuoted(), ","))
            .Property("item_number", V.String().SqlQuoted())
            .Property("type", V.String().SqlQuoted(false));
        string aml = $"<Item id='{FirstId}' idList='{FirstId},{SecondId}'>" +
            "<item_number>ABC123</item_number><type>ABC</type></Item>";

        foreach (object source in new object[] { aml, ArasIntegrationTests.ItemFrom(aml) })
        {
            V.AssertInput(schema, source);
            var (item, inn) = V.ParseInput(schema, source);
            Assert.Equal(FirstId, (object)item.id);
            var ids = Assert.IsType<string[]>((object)item["@idList"]);
            Assert.Collection(ids, value => Assert.Equal("'" + FirstId + "'", value),
                value => Assert.Equal("'" + SecondId + "'", value));
            Assert.Equal("'ABC123'", (object)item.item_number);
            Assert.Equal("ABC", (object)item.type);
            Assert.Equal(source is Item, inn is Innovator);
        }
    }

    [Fact]
    public void DelimitedFieldsUseTypedArraysAndPreserveElementOrder()
    {
        var schema = V.Item().Property("ids", V.Array(V.Guid(), ","))
            .Property("weights", V.Array(V.Integer().Min(0), "||"));
        string aml = $"<Item><ids>{FirstId},{SecondId},{FirstId}</ids>" +
            "<weights>4||9223372036854775807||0</weights></Item>";

        var (item, _) = V.ParseInput(schema, aml);

        string[] ids = Assert.IsType<string[]>((object)item.ids);
        Assert.Collection(ids, value => Assert.Equal(FirstId, value),
            value => Assert.Equal(SecondId, value), value => Assert.Equal(FirstId, value));
        long[] weights = Assert.IsType<long[]>((object)item.weights);
        Assert.Collection(weights, value => Assert.Equal(4L, value),
            value => Assert.Equal(long.MaxValue, value), value => Assert.Equal(0L, value));
    }

    [Fact]
    public void AFieldMustBePresentUnlessOptionalAndCanContainAnEmptyArray()
    {
        var array = V.Array(V.Integer(), ",");
        var required = V.Item().Property("weights", array);
        var optional = V.Item().Property("weights", array.Optional());

        Assert.Throws<ValidationException>(() => V.ParseInput(required, "<Item/>"));
        Assert.Null((object?)V.ParseInput(optional, "<Item/>").Input.weights);
        Assert.Null((object?)V.ParseInput(optional, "<Item><weights is_null='1'/></Item>")
            .Input.weights);
        var (item, _) = V.ParseInput(required, "<Item><weights/></Item>");
        Assert.Empty(Assert.IsType<long[]>((object)item.weights));
    }

    [Fact]
    public void InvalidAndEmptyTokensRetainTheirIndexesInABatch()
    {
        var schema = V.Items(V.Item().Attribute("idList", V.Array(V.Guid().SqlQuoted(), ",")));
        string aml = $"<AML><Item idList='{FirstId}'/><Item idList=',{FirstId},bad,'/></AML>";
        var asserted = Assert.Throws<ValidationException>(() => V.AssertInput(schema, aml));
        var parsed = Assert.Throws<ValidationException>(() => V.ParseInput(schema, aml));

        Assert.Equal(asserted.GetErrorString(), parsed.GetErrorString());
        Assert.Collection(parsed.Issues,
            issue => Assert.Equal("$[1].@idList[0]", issue.Path),
            issue => Assert.Equal("$[1].@idList[2]", issue.Path),
            issue => Assert.Equal("$[1].@idList[3]", issue.Path));
        Assert.DoesNotContain("Row ", parsed.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DelimitersAreLiteralAndBoundsApplyToTheElementCount()
    {
        var schema = V.Item().Property("names", V.Array(V.String(), ".").Min(2).Max(2));
        var (item, _) = V.ParseInput(schema, "<Item><names>A. B</names></Item>");
        var names = Assert.IsType<string[]>((object)item.names);
        Assert.Collection(names, value => Assert.Equal("A", value),
            value => Assert.Equal(" B", value));

        Assert.Throws<ArgumentException>(() => V.Array(V.Integer(), ""));
        var tooFew = Assert.Throws<ValidationException>(() =>
            V.AssertInput(schema, "<Item><names>A</names></Item>"));
        Assert.Equal("$.names", Assert.Single(tooFew.Issues).Path);
        Assert.Throws<ValidationException>(() =>
            V.AssertInput(schema, "<Item><names>A.B.C</names></Item>"));
    }

    [Fact]
    public void DelimitedFieldEnforcesTheGlobalElementLimit()
    {
        var schema = V.Item().Property("counts", V.Array(V.Integer(), ","));
        string values = string.Join(",", Enumerable.Repeat("1", Limits.Items));
        V.AssertInput(schema, "<Item><counts>" + values + "</counts></Item>");

        var error = Assert.Throws<ValidationException>(() =>
            V.AssertInput(schema, "<Item><counts>" + values + ",1</counts></Item>"));
        Assert.Equal("$.counts", Assert.Single(error.Issues).Path);
    }

    [Fact]
    public void GuidBuilderValidatesItsChoicesAndDefaultBeforeSqlQuoting()
    {
        Assert.Throws<ArgumentException>(() => V.Guid().OneOf("bad"));
        Assert.Throws<ArgumentException>(() => V.Guid().SqlQuoted().Default("bad"));

        var schema = V.Item().Property("related_id", V.Guid().SqlQuoted(false).Default(SecondId));
        var (item, _) = V.ParseInput(schema, "<Item/>");
        Assert.Equal(SecondId, (object)item.related_id);
    }
}
