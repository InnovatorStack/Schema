using System;
using Xunit;
using V = InnovatorSchema.Validator;

namespace InnovatorSchema.Tests;

public sealed class BuilderTests
{
    [Theory]
    [InlineData("", null)]
    [InlineData("<height is_null='1'/>", null)]
    [InlineData("<height/>", "")]
    [InlineData("<height></height>", "")]
    [InlineData("<height is_null='0'/>", "")]
    [InlineData("<height> 12 mm </height>", " 12 mm ")]
    public void OptionalStringPreservesTheSuppliedValue(string property, string? expected)
    {
        var schema = V.Item().Property("height", V.String().Optional());

        foreach (object input in Inputs(property))
        {
            V.AssertInput(schema, input);
            var item = V.ParseInput(schema, input);
            Assert.Equal(expected, (object?)item.height);
            Assert.Equal(expected, (object?)item["height"]);
        }
    }

    [Theory]
    [InlineData("", "user")]
    [InlineData("<type is_null='1'/>", "user")]
    [InlineData("<type>identity</type>", "identity")]
    public void EnumDefaultAppliesToAbsentAndNullValues(string property, string expected)
    {
        var schema = V.Item().Property("type", V.String().Enum("user", "identity").Default("user"));

        foreach (object input in Inputs(property))
        {
            V.AssertInput(schema, input);
            object value = V.ParseInput(schema, input).type;
            Assert.Equal(expected, Assert.IsType<string>(value));
        }
    }

    [Theory]
    [InlineData("<type/>")]
    [InlineData("<type>admin</type>")]
    [InlineData("<type>User</type>")]
    [InlineData("<type is_null='1'>user</type>")]
    public void DefaultDoesNotReplaceSuppliedInvalidValues(string property)
    {
        var schema = V.Item().Property("type", V.String().Enum("user", "identity").Default("user"));
        RejectInBothEntryPoints(schema, property, "$.type");
    }

    [Theory]
    [InlineData("<height><![CDATA[ ]]></height>")]
    [InlineData("<height> </height>")]
    [InlineData("<height is_null='yes'/>")]
    [InlineData("<height is_null='1'>12 mm</height>")]
    public void OptionalDoesNotReplaceInvalidTextWithNull(string property)
    {
        RejectInBothEntryPoints(V.Item().Property("height", V.String().Optional()), property,
            "$.height");
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("<weight is_null='1'/>", null)]
    [InlineData("<weight>1.25</weight>", 1.25)]
    public void OptionalNumberReadsNullOrDouble(string property, double? expected)
    {
        var schema = V.Item().Property("weight", V.Double().Optional().Min(0.0));

        foreach (object input in Inputs(property))
        {
            V.AssertInput(schema, input);
            Assert.Equal(expected, (object?)V.ParseInput(schema, input).weight);
        }
    }

    [Theory]
    [InlineData("<weight/>")]
    [InlineData("<weight> </weight>")]
    [InlineData("<weight>-1.25</weight>")]
    public void OptionalNumberRejectsBlankOrOutOfRangeInput(string property)
    {
        RejectInBothEntryPoints(V.Item().Property("weight", V.Double().Optional().Min(0.0)),
            property, "$.weight");
    }

    [Fact]
    public void InvalidDefaultsFailWhileDefiningTheScalar()
    {
        Assert.Throws<ArgumentException>(() =>
            V.String().Enum("user", "identity").Default("admin"));
        Assert.Throws<ArgumentException>(() =>
            V.String().Default("admin").Enum("user", "identity"));
        Assert.Throws<ArgumentException>(() => V.String().Length(1, 3).Default("abcd"));
        Assert.Throws<ArgumentException>(() => V.String().Default(" "));
        Assert.Throws<ArgumentException>(() => V.Double().Min(0.0).Default(-1.0));
        Assert.Throws<ArgumentException>(() => V.Double().Default(double.NaN));
        Assert.Throws<ArgumentException>(() => V.Guid().Default(Guid.Empty));
    }

    [Fact]
    public void DefaultsAreTypedAndDoNotChangeTheInput()
    {
        var schema = V.Item().Property("quantity", V.Integer().Min(1).Default(2))
            .Property("height", V.String().Optional().Length(0).Default(""));

        var input = ArasIntegrationTests.ItemFrom("<Item><quantity is_null='1'/></Item>");
        string before = input.dom.OuterXml;
        var item = V.ParseInput(schema, input);
        object quantity = item.quantity;
        object height = item.height;

        Assert.Equal(2L, Assert.IsType<long>(quantity));
        Assert.Equal("", Assert.IsType<string>(height));
        Assert.Equal(before, input.dom.OuterXml);
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("<name>AB</name>", "AB")]
    [InlineData("<name>ABC</name>", "ABC")]
    public void OptionalStringLengthIncludesBothEndpoints(string property, string? expected)
    {
        var schema = V.Item().Property("name", V.String().Optional().Length(2, 3));

        foreach (object input in Inputs(property))
        {
            Assert.Equal(expected, (object?)V.ParseInput(schema, input).name);
        }
    }

    [Fact]
    public void ExactAndOneSidedLengthsUseTheSuppliedString()
    {
        var exact = V.Item().Property("name", V.String().Length(2));
        var bounded = V.Item().Property("name", V.String().Optional().MinLength(2).MaxLength(3));

        V.AssertInput(exact, "<Item><name>AB</name></Item>");
        RejectInBothEntryPoints(exact, "<name>ABC</name>", "$.name");
        RejectInBothEntryPoints(bounded, "<name/>", "$.name");
        RejectInBothEntryPoints(bounded, "<name>ABCD</name>", "$.name");
        Assert.Throws<ArgumentOutOfRangeException>(() => V.String().Length(3, 2));
    }

    [Fact]
    public void LegacyNullishContinuesReturningNullForBlankText()
    {
#pragma warning disable CS0618
        var schema = V.Item().Property("name", V.String().Nullish());
#pragma warning restore CS0618
        Assert.Null((object?)V.ParseInput(schema, "<Item><name/></Item>").name);
    }

    private static object[] Inputs(string property)
    {
        string aml = "<Item>" + property + "</Item>";
        return new object[] { aml, ArasIntegrationTests.ItemFrom(aml) };
    }

    private static void RejectInBothEntryPoints(ItemSchema schema, string property, string path)
    {
        foreach (object input in Inputs(property))
        {
            var asserted = Assert.Throws<ValidationException>(() => V.AssertInput(schema, input));
            var parsed = Assert.Throws<ValidationException>(() => V.ParseInput(schema, input));
            Assert.Equal(path, Assert.Single(parsed.Issues).Path);
            Assert.Equal(asserted.Message, parsed.Message);
            Assert.Equal(asserted.GetErrorString(), parsed.GetErrorString());
        }
    }
}
