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
        Assert.Throws<ValidationException>(() => V.AssertInput(schema, deep));
        Assert.Throws<ValidationException>(() =>
            V.AssertInput(schema, "<Item>" + new string('x', 1024 * 1024) + "</Item>"));
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
