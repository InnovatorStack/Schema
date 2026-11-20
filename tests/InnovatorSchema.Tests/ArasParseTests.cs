#if ARAS_TESTS
using System;
using System.Threading.Tasks;
using Aras.IOM;
using InnovatorSchema.Testing;
using Xunit;
using V = InnovatorSchema.Validator;

namespace InnovatorSchema.Tests;

public sealed class ArasParseTests
{
    [Fact]
    public void ConcurrentItemsKeepTheirOwnXmlAndParsedData()
    {
        var schema = Part();
        Parallel.For(0, 100, index =>
        {
            string number = "P-" + index;
            var item = Load("<Item type='Part'><item_number>" + number +
                "</item_number></Item>");
            var output = Present((object?)V.Parse(schema, item));
            string actual = output.item_number;
            Assert.Equal(number, actual);
            Assert.Equal(number, item.getProperty("item_number"));
        });
    }

    private const string Parts = "<AML><Item type='Part'><item_number>P-100</item_number>" +
        "</Item><Item type='Part'><item_number>P-200</item_number></Item></AML>";

    [Fact]
    public void WholeIomCollectionProducesNamedRowsInSourceOrderWithoutMutation()
    {
        var items = Load(Parts);
        string original = items.ToString();
        var schema = V.Array(Part()).MinCount(1);
        var result = V.SafeParse(schema, items);
        Assert.True(result.Success, result.GetErrorString());
        var output = Present((object?)result.Data);
        Assert.NotNull(output);
        string first = output[0].item_number;
        string second = output[1].item_number;
        int count = output.Count;
        Assert.Equal("P-100", first);
        Assert.Equal("P-200", second);
        Assert.Equal(2, count);
        Assert.Equal(original, items.ToString());
    }

    [Fact]
    public void AmlCollectionAndIomCollectionHaveTheSameOutput()
    {
        var schema = V.Array(Part());
        var aml = V.SafeParse(schema, Parts, InputFormat.Aml);
        var iom = V.SafeParse(schema, Load(Parts));
        Assert.True(aml.Success, aml.GetErrorString());
        Assert.True(iom.Success, iom.GetErrorString());
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize((object?)aml.Data),
            System.Text.Json.JsonSerializer.Serialize((object?)iom.Data));
    }

    [Theory]
    [InlineData("<AML/>", 0)]
    [InlineData("<AML><Item type='Part'><item_number>P-100</item_number></Item></AML>", 1)]
    [InlineData("<Item type='Part'><item_number>P-100</item_number></Item>", 1)]
    public void EmptyAndSingletonCollectionsKeepTheirCardinality(string input, int expected)
    {
        var schema = V.Array(Part());
        var aml = Present((object?)V.Parse(schema, input, InputFormat.Aml));
        var iom = Present((object?)V.Parse(schema, Load(input)));
        Assert.NotNull(aml);
        Assert.NotNull(iom);
        int amlCount = aml.Count;
        int iomCount = iom.Count;
        Assert.Equal(expected, amlCount);
        Assert.Equal(expected, iomCount);
    }

    [Fact]
    public void SingleSchemaRejectsMultipleRootsWithAValidationError()
    {
        Assert.Equal(IssueCode.Cardinality,
            Assert.Single(V.SafeParse(Part(), Load(Parts)).Issues).Code);
        Assert.Equal(IssueCode.Cardinality,
            Assert.Single(V.SafeParse(Part(), Parts, InputFormat.Aml).Issues).Code);
        Assert.Throws<ValidationException>(() => V.Parse(Part(), Load(Parts)));
    }

    [Fact]
    public void CollectionTransformsRetainTheCollectionInputContract()
    {
        var schema = V.Array(Part()).MinCount(1)
            .Check(rows => rows.Count <= 5, "rows", "Use at most five rows.")
            .Transform(rows => rows.Count);
        Assert.Equal(2, (object?)V.Parse(schema, Load(Parts)));
        Assert.Equal(2, (object?)V.Parse(schema, Parts, InputFormat.Aml));
    }

    [Fact]
    public void MultipleInvalidRowsReportEveryIndependentIndexedIssue()
    {
        var input = Load("<AML><Item type='Part'><item_number> </item_number></Item>" +
            "<Item type='Part'/></AML>");
        var result = V.SafeParse(V.Array(Part()), input);
        Assert.Equal(2, result.Issues.Count);
        Assert.Equal("$[0].item_number", result.Issues[0].Path.ToString());
        Assert.Equal("$[1].item_number", result.Issues[1].Path.ToString());
        Assert.NotNull(result.Error);
        Assert.Equal(result.GetErrorString(), result.Error.GetErrorString());
        Assert.Equal(typeof(SafeParseResult), result.GetType());
    }

    [Fact]
    public void CollectionLimitsApplyToTheCompleteSelection()
    {
        var schema = V.Array(Part());
        var collection = V.SafeParse(schema, Load(Parts),
            new ValidationOptions(maximumCollectionLength: 1));
        Assert.Equal(IssueCode.LimitExceeded, Assert.Single(collection.Issues).Code);
        var nodes = V.SafeParse(schema, Load(Parts), new ValidationOptions(maximumNodes: 5));
        Assert.Equal(IssueCode.LimitExceeded, Assert.Single(nodes.Issues).Code);
        Assert.StartsWith("$[1]", nodes.Issues[0].Path.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void FaultItemsAreNotEmptyCollections()
    {
        var fault = OfflineConnection.CreateInnovator().newError("Existing fault.");
        var result = V.SafeParse(V.Array(Part()), fault);
        Assert.Equal(IssueCode.Cardinality, Assert.Single(result.Issues).Code);
    }

    [Theory]
    [InlineData("<AML><Other/></AML>")]
    [InlineData("<AML>text<Item type='Part'/></AML>")]
    [InlineData("<AML is_null='bad'><Item type='Part'/></AML>")]
    [InlineData("<AML is_null='1'><Item type='Part'/></AML>")]
    public void MalformedAmlCollectionsFailWithoutThrowing(string input)
    {
        Assert.False(V.SafeParse(V.Array(Part()), input, InputFormat.Aml).Success);
    }

    [Fact]
    public void ParsedItemsExposeDeclaredAttributesExpansionAndRelationships()
    {
        var line = V.Item("Part BOM").Property("quantity", V.DecimalText().Min(0m))
            .Property("related_id", Part());
        var schema = Part().Property(V.Attribute("action", V.String()))
            .Property(V.Relationships("Part BOM", V.Array(line)));
        var input = Load("<Item type='Part' action='add'><item_number>P-100</item_number>" +
            "<extra>ignored</extra><Relationships><Item type='Other'/>" +
            "<Item type='Part BOM'><quantity>2.5</quantity><related_id>" +
            "<Item type='Part'><item_number>P-200</item_number></Item></related_id>" +
            "</Item></Relationships></Item>");
        var output = Present((object?)V.Parse(schema, input));
        Assert.NotNull(output);
        string action = output["@action"];
        string related = output.Relationships["Part BOM"][0].related_id.item_number;
        decimal quantity = output.Relationships["Part BOM"][0].quantity;
        Assert.Equal("add", action);
        Assert.Equal("P-200", related);
        Assert.Equal(2.5m, quantity);
        Assert.False((bool)output.ContainsKey("extra"));
    }

    [Theory]
    [InlineData("<Relationships>text</Relationships>")]
    [InlineData("<Relationships><Other/></Relationships>")]
    [InlineData("<Relationships><Item/></Relationships>")]
    [InlineData("<Relationships is_null='bad'/>")]
    [InlineData("<Relationships is_null='1'><Item type='Part BOM'/></Relationships>")]
    public void InvalidRelationshipContainersAreNotSilentlyIgnored(string container)
    {
        var schema = Part().Property(V.Relationships("Part BOM", V.Array(V.Item("Part BOM"))));
        string aml = "<Item type='Part'><item_number>P-100</item_number>" + container + "</Item>";
        var result = V.SafeParse(schema, aml, InputFormat.Aml);
        Assert.False(result.Success);
        Assert.Equal("$.Relationships[\"Part BOM\"]", Assert.Single(result.Issues).Path.ToString());
    }

    [Fact]
    public void OutputBindingCollisionsFailDuringDefinitionConstruction()
    {
        var shape = V.Item("Part").Property("Relationships", V.Object());
        Assert.Throws<ArgumentException>(() => shape.Property(
            V.Relationships("Part BOM", V.Array(V.Item("Part BOM")))));
        Assert.Throws<ArgumentException>(() => V.Object()
            .Property("@action", V.String()).Property(V.Attribute("action", V.String())));
    }

    private static dynamic Present(object? output)
    {
        Assert.NotNull(output);
        return output;
    }

    private static ItemSchema Part()
    {
        return V.Item("Part").Property("item_number", V.String().NotBlank());
    }

    private static Item Load(string aml)
    {
        var item = OfflineConnection.CreateInnovator().newItem();
        item.loadAML(aml);
        return item;
    }
}
#endif
