using System;
using System.Globalization;
using System.Linq;
using Xunit;
using V = InnovatorSchema.Validator;

namespace InnovatorSchema.Tests;

public sealed class AssertionTests
{
    private static readonly string[] BatchIssuePaths =
        { "$[0].@id", "$[0].@action", "$[0].component.@id", "$[0].weight", "$[1].@type" };

    [Theory]
    [InlineData("", false)]
    [InlineData("<name is_null='1'/>", false)]
    [InlineData("<name/>", false)]
    [InlineData("<name> \t </name>", false)]
    [InlineData("<name>Bracket</name>", true)]
    public void DeclaredPropertiesRejectNullishByDefault(string property, bool valid)
    {
        string aml = "<Item type='Part'>" + property + "<other>ignored</other></Item>";
        var schema = V.Item("Part").Property("name", V.String().MaxLength(8));
        Exception? error = Record.Exception(() => V.AssertInput(schema, aml));
        Assert.Equal(valid, error is null);

        if (!valid)
        {
            Assert.Equal("$.name", Assert.Single(Assert.IsType<ValidationException>(error)
                .Issues).Path);
        }

        V.AssertInput(V.Item("Part").Property("name", V.String().MaxLength(8).Nullish()), aml);
    }

    [Theory]
    [InlineData("<n>ten</n>")]
    [InlineData("<n><Item/></n>")]
    [InlineData("<n is_null='1'>42</n>")]
    [InlineData("<n is_null='yes'/>")]
    [InlineData("<n>9</n><n>10</n>")]
    public void NullishDoesNotAllowInvalidSuppliedData(string property)
    {
        var schema = V.Item().Property("n", V.Integer().Nullish());
        var error = Assert.Throws<ValidationException>(() =>
            V.AssertInput(schema, "<Item>" + property + "</Item>"));
        Assert.Equal("$.n", Assert.Single(error.Issues).Path);
    }

    [Theory]
    [InlineData("<Item id='123'/>", true)]
    [InlineData("<Item><id condition='eq'> 123 </id></Item>", true)]
    [InlineData("<Item id='123'><id>different</id></Item>", true)]
    [InlineData("<Item><id condition='in'>123</id></Item>", false)]
    [InlineData("<Item id=' '><id>123</id></Item>", false)]
    [InlineData("<Item type='Part'/>", false)]
    public void IdFollowsIomLookupAndRequiresOnlyPresence(string aml, bool valid)
    {
        Exception? error = Record.Exception(() => V.AssertInput(V.Item().ID(), aml));
        Assert.Equal(valid, error is null);

        if (!valid)
        {
            Assert.Equal("$.@id", Assert.Single(Assert.IsType<ValidationException>(error)
                .Issues).Path);
        }
    }

    [Theory]
    [InlineData("783BA98DA5D04BF9AF4B1760826221F0", true)]
    [InlineData("783ba98da5d01bf9af4b1760826221f0", true)]
    [InlineData("783BA98D-A5D0-4BF9-AF4B-1760826221F0", false)]
    [InlineData("{783BA98D-A5D0-4BF9-AF4B-1760826221F0}", false)]
    [InlineData("00000000000000000000000000000000", false)]
    [InlineData("783BA98DA5D04BF9AF4B1760826221FG", false)]
    [InlineData(" 783BA98DA5D04BF9AF4B1760826221F0", false)]
    [InlineData("783BA98DA5D04BF9AF4B1760826221F", false)]
    [InlineData("", false)]
    public void GuidChecksTheArasFormatForIdsAndProperties(string id, bool valid)
    {
        var schema = V.Item("Part").ID(V.Guid()).ID().Property("related_id", V.Guid());
        string aml = $"<Item type='Part' id='{id}'><related_id>{id}</related_id></Item>";
        Exception? error = Record.Exception(() => V.AssertInput(schema, aml));

        if (valid)
        {
            Assert.Null(error);
        }
        else
        {
            Assert.Collection(Assert.IsType<ValidationException>(error).Issues,
                issue => Assert.Equal("$.@id", issue.Path),
                issue => Assert.Equal("$.related_id", issue.Path));
        }
    }

    [Fact]
    public void GuidAllowsNullishPropertiesAndRequiresIds()
    {
        var guid = V.Guid().Nullish();
        V.AssertInput(V.Item().Property("related_id", guid), "<Item/>");
        Assert.Throws<ArgumentException>(() => V.Item().ID(guid));
    }

    [Fact]
    public void ExceptionMessageKeepsFieldNamesAndCustomRuleText()
    {
        var schema = V.Item("Part").Property("weight", V.Integer().Min(3))
            .Property("item_number", V.String().MaxLength(3, "Use a shorter part number."));
        const string aml = "<Item type='Part'><weight>2</weight>" +
            "<item_number>ABCD</item_number></Item>";
        var error = Assert.Throws<ValidationException>(() => V.AssertInput(schema, aml));

        Assert.Equal("Weight must be at least 3.\nUse a shorter part number.",
            error.Message);
        Assert.Contains("$.weight: Must be at least 3.", error.GetErrorString(),
            StringComparison.Ordinal);
        Assert.Equal(2, error.Issues.Count);
    }

    [Fact]
    public void InvalidIntegerDisplaysAWholeNumberMessage()
    {
        var schema = V.Item("Part").Property("weight", V.Integer().Nullish().Min(3));
        const string aml = "<Item type='Part'><weight>heavy</weight></Item>";
        var error = Assert.Throws<ValidationException>(() => V.AssertInput(schema, aml));

        Assert.Equal("Weight must be a whole number.", error.Message);
        Assert.Contains("$.weight: Expected a signed 64-bit integer.", error.GetErrorString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void ScalarFormatErrorsTellTheUserWhatToEnter()
    {
        var schema = V.Item("Part").ID(V.Guid())
            .Property("weight", V.Double())
            .Property("effective_date", V.Date());
        const string aml = "<Item type='Part' id='abc'><weight>1,5</weight>" +
            "<effective_date>tomorrow</effective_date></Item>";
        var error = Assert.Throws<ValidationException>(() => V.AssertInput(schema, aml));

        Assert.Equal("ID must be a valid Aras ID.\n" +
            "Weight must be a number. Use a period for decimals.\n" +
            "Effective date must be a valid date, such as 2026-01-15.", error.Message);
        Assert.Collection(error.Issues,
            issue => Assert.Equal("Expected a nonzero, 32-character Aras GUID.", issue.Message),
            issue => Assert.Equal("Expected a finite number in AML format.", issue.Message),
            issue => Assert.Equal("Expected yyyy-MM-dd or yyyy-MM-ddTHH:mm:ss.", issue.Message));
    }

    [Fact]
    public void InvalidNullMarkerNamesTheFieldInTheExceptionMessage()
    {
        var schema = V.Item().Property("weight", V.Integer().Nullish());
        const string aml = "<Item><weight is_null='yes'/></Item>";
        var error = Assert.Throws<ValidationException>(() => V.AssertInput(schema, aml));

        Assert.Equal("Weight: Check whether this value should be blank.", error.Message);
        Assert.Equal("is_null must be 0 or 1.", Assert.Single(error.Issues).Message);
    }

    [Fact]
    public void UserMessagesIdentifyTheCollectionRowAndNestedField()
    {
        var part = V.Item().Property("weight", V.Integer().Min(3).Nullish());
        var schema = V.Array(V.Item().Property("related_id", part));
        const string aml = "<AML><Item><related_id><Item/></related_id></Item>" +
            "<Item><related_id><Item><weight>2</weight></Item></related_id></Item></AML>";
        var error = Assert.Throws<ValidationException>(() => V.AssertInput(schema, aml));

        Assert.Equal("Row 2: Related id: Weight must be at least 3.", error.Message);
        Assert.Equal("$[1].related_id.weight", Assert.Single(error.Issues).Path);
    }

    [Fact]
    public void EachArrayRowAndNestedPropertyHasAnIssuePath()
    {
        var schema = V.Array(V.Item("Part").ID()
            .Attribute("action", V.Enum("add", "edit", "update"))
            .Property("component", V.Item("Part").ID())
            .Property("weight", V.Integer().Min(0, "Use a nonnegative weight.")));
        const string aml = "<AML><Item type='Part' action='get'><component><Item type='Part'/>" +
            "</component><weight>-1</weight></Item><Item type='Document'/></AML>";

        var error = Assert.Throws<ValidationException>(() => V.AssertInput(schema, aml));

        Assert.Equal(BatchIssuePaths, error.Issues.Select(issue => issue.Path));
        Assert.Contains("Row 2: Send an item of type Part.", error.Message,
            StringComparison.Ordinal);
        Assert.Contains("Use a nonnegative weight.", error.GetErrorString(),
            StringComparison.Ordinal);
        Assert.Same(error.Issues, error.GetIssues());
    }

    [Fact]
    public void NestedItemsFollowTheDeclaredPropertyName()
    {
        var schema = V.Item("Part").Property("some_property", V.Item().ID());
        V.AssertInput(schema, "<Item type='Part'><some_property><Item type='Document' id='a'/>" +
            "</some_property></Item>");

        var error = Assert.Throws<ValidationException>(() =>
            V.AssertInput(schema, "<Item type='Part'><some_property type='Part'>a" +
                "</some_property></Item>"));
        Assert.Equal("Some property must include one related item.", error.Message);
        Assert.Contains("expanded Item", Assert.Single(error.Issues).Message,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("9223372036854775808", "1.0", "2026-11-23")]
    [InlineData("1.0", "1.0", "2026-11-23")]
    [InlineData("1", "NaN", "2026-11-23")]
    [InlineData("1", "1e999", "2026-11-23")]
    [InlineData("1", "1,5", "2026-11-23")]
    [InlineData("1", "1.0", "2026-02-30")]
    [InlineData("1", "1.0", "23.11.2026")]
    public void ScalarsRejectInvalidAml(string count, string weight, string date)
    {
        string aml = ScalarAml(count, weight, date);
        Assert.Single(Assert.Throws<ValidationException>(() =>
            V.AssertInput(ScalarShape(), aml)).Issues);
    }

    [Fact]
    public void ScalarsUseInvariantFormatsUnderDifferentCultures()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

        try
        {
            V.AssertInput(ScalarShape(), ScalarAml("1", "1.5", "2026-11-23T17:40:00"));
            V.AssertInput(ScalarShape(), ScalarAml("0", "0", "2026-11-23"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData("{\"type\":\"Part\"}")]
    [InlineData("<object/>")]
    [InlineData("<Item")]
    [InlineData("<!DOCTYPE Item [<!ENTITY text 'value'>]><Item>&text;</Item>")]
    [InlineData("<AML>stray text<Item/></AML>")]
    public void RejectsInputOutsideTheAmlContract(string input)
    {
        Assert.Throws<ValidationException>(() => V.AssertInput(V.Item(), input));
    }

    [Fact]
    public void CollectionCardinalityIsCheckedBeforeRows()
    {
        var item = V.Item("Part");
        var array = V.Array(item);
        V.AssertInput(array, "<AML/>");
        V.AssertInput(array, "<Item type='Part'/>");
        V.AssertInput(item, "<AML><Item type='Part'/></AML>");
        Assert.Throws<ValidationException>(() => V.AssertInput(item, "<AML/>"));
        Assert.Throws<ValidationException>(() => V.AssertInput(item, "<AML><Item/><Item/></AML>"));
        Assert.Throws<ValidationException>(() => V.AssertInput(array.MinItems(1), "<AML/>"));
        Assert.Throws<ValidationException>(() =>
            V.AssertInput(array.MaxItems(0), "<Item type='Part'/>"));
    }

    [Fact]
    public void MalformedAndExcessiveInputsFailWithValidationException()
    {
        var schema = V.Item();
        string deep = "<Item>" + string.Concat(Enumerable.Repeat("<x>", 70)) +
            string.Concat(Enumerable.Repeat("</x>", 70)) + "</Item>";
        var deepError = Assert.Throws<ValidationException>(() => V.AssertInput(schema, deep));
        var sizeError = Assert.Throws<ValidationException>(() =>
            V.AssertInput(schema, "<Item>" + new string('x', 1024 * 1024) + "</Item>"));

        Assert.Equal("The request could not be read. Check its format and size.",
            deepError.Message);
        Assert.Equal("The request is too large. Reduce the amount of data and try again.",
            sizeError.Message);
        Assert.Equal("AML length limit exceeded.", Assert.Single(sizeError.Issues).Message);
    }

    private static ItemSchema ScalarShape()
    {
        return V.Item("Part")
            .Property("count", V.Integer().Min(0))
            .Property("weight", V.Double().Min(0.0).Max(10.0))
            .Property("date", V.Date());
    }

    private static string ScalarAml(string count, string weight, string date)
    {
        return $"<Item type='Part'><count>{count}</count><weight>{weight}</weight>" +
            $"<date>{date}</date></Item>";
    }
}
