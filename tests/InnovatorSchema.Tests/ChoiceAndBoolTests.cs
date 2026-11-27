using System;
using Xunit;
using V = InnovatorSchema.Validator;

namespace InnovatorSchema.Tests;

public sealed class ChoiceAndBoolTests
{
    [Theory]
    [InlineData("0", false)]
    [InlineData("1", true)]
    public void BoolReturnsTheParsedBoolean(string text, bool expected)
    {
        var schema = V.Item().Property("is_test", V.Bool());
        string aml = "<Item><is_test>" + text + "</is_test></Item>";

        foreach (object input in new object[] { aml, ArasIntegrationTests.ItemFrom(aml) })
        {
            V.AssertInput(schema, input);
            var (parsed, _) = V.ParseInput(schema, input);
            Assert.Equal(expected, Assert.IsType<bool>((object)parsed.is_test));
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData(" 1 ")]
    [InlineData("01")]
    [InlineData("2")]
    [InlineData("true")]
    [InlineData("false")]
    public void BoolRejectsOtherSuppliedSpellings(string text)
    {
        var schema = V.Item().Property("is_test", V.Bool().Default(false));
        string aml = "<Item><is_test>" + text + "</is_test></Item>";
        var error = Assert.Throws<ValidationException>(() => V.ParseInput(schema, aml));

        Assert.Equal("Is test must be 0 or 1.", error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("<is_test is_null='1'/>")]
    public void BoolDistinguishesAnOptionalNullFromAFalseDefault(string property)
    {
        string aml = "<Item>" + property + "</Item>";
        var optional = V.Item().Property("is_test", V.Bool().Optional());
        var withDefault = V.Item().Property("is_test", V.Bool().Default(false));

        Assert.Null((object?)V.ParseInput(optional, aml).Input.is_test);
        Assert.False(Assert.IsType<bool>((object)V.ParseInput(withDefault, aml).Input.is_test));
    }

    [Fact]
    public void OneOfComparesParsedScalars()
    {
        const string id = "783BA98DA5D04BF9AF4B1760826221F0";
        var date = new DateTime(2026, 11, 27);
        var schema = V.Item().Property("quantity", V.Integer().OneOf(1, 3, 5))
            .Property("weight", V.Double().OneOf(1.5, 2.0))
            .Property("effective_date", V.Date().OneOf(date))
            .Property("reference", V.Guid().OneOf(Guid.ParseExact(id, "N")))
            .Property("is_test", V.Bool().OneOf(true));
        string aml = "<Item><quantity>03</quantity><weight>1.50</weight>" +
            "<effective_date>2026-11-27T00:00:00</effective_date>" +
            $"<reference>{id.ToLowerInvariant()}</reference><is_test>1</is_test></Item>";

        var (parsed, _) = V.ParseInput(schema, aml);
        Assert.Equal(3L, Assert.IsType<long>((object)parsed.quantity));
        Assert.Equal(1.5, Assert.IsType<double>((object)parsed.weight));
        Assert.Equal(date, Assert.IsType<DateTime>((object)parsed.effective_date));
        Assert.Equal(Guid.ParseExact(id, "N"), Assert.IsType<Guid>((object)parsed.reference));
        Assert.True(Assert.IsType<bool>((object)parsed.is_test));
        var error = Assert.Throws<ValidationException>(() => V.AssertInput(schema,
            aml.Replace("<quantity>03", "<quantity>2", StringComparison.Ordinal)
                .Replace("<is_test>1", "<is_test>0", StringComparison.Ordinal)));
        Assert.Collection(error.Issues, issue => Assert.Equal("$.quantity", issue.Path),
            issue => Assert.Equal("$.is_test", issue.Path));
    }

    [Fact]
    public void InsensitivePreservesTextAndOnlyChangesTheLastChoiceRule()
    {
        var exact = V.String().OneOf("user", "identity");
        var insensitive = V.Item().Property("type", exact.Insensitive().Default("USER"));
        var both = V.Item().Property("type", exact.OneOf("user", "identity").Insensitive());
        const string aml = "<Item><type>Identity</type></Item>";

        Assert.Equal("Identity", (object)V.ParseInput(insensitive, aml).Input.type);
        Assert.Equal("USER", (object)V.ParseInput(insensitive, "<Item/>").Input.type);
        Assert.Throws<ValidationException>(() => V.AssertInput(both, aml));
        Assert.Throws<ValidationException>(() => V.AssertInput(V.Item().Property("type", exact),
            aml));
        Assert.Throws<ArgumentException>(() => exact.Insensitive().Default("admin"));
    }

    [Fact]
    public void ChoiceRulesRejectInvalidConfigurationAndCopyTheirChoices()
    {
        Assert.Throws<ArgumentException>(() => V.String().OneOf());
        Assert.Throws<ArgumentException>(() => V.String().OneOf("user", "user"));
        Assert.Throws<ArgumentException>(() => V.Double().OneOf(double.NaN));
        Assert.Throws<InvalidOperationException>(() => V.String().Insensitive());
        Assert.Throws<InvalidOperationException>(() => V.String().OneOf("user", "USER")
            .Insensitive());
        Assert.Throws<InvalidOperationException>(() => V.String().OneOf("user").MaxLength(5)
            .Insensitive());

        string[] choices = { "user", "identity" };
        var schema = V.Item().Property("type", V.String().OneOf(choices).Insensitive());
        choices[0] = "admin";
        V.AssertInput(schema, "<Item><type>USER</type></Item>");
        Assert.Throws<ValidationException>(() => V.AssertInput(schema,
            "<Item><type>admin</type></Item>"));
    }
}
