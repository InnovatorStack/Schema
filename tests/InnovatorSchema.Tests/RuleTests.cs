using System;
using System.Globalization;
using System.Text.Json;
using Xunit;

namespace InnovatorSchema.Tests;

public sealed class RuleTests
{
    [Theory]
    [InlineData("", false)]
    [InlineData(" \t", false)]
    [InlineData(" A ", true)]
    [InlineData("😀", true)]
    public void StringRulesPreserveContentAndCountUtf16Units(string value, bool expected)
    {
        var schema = Validator.String().NotBlank().MinLength(2).MaxLength(3);
        var result = Validator.ValidateJson(schema, JsonSerializer.Serialize(value));

        Assert.Equal(expected, result.IsValid);

        if (expected)
        {
            Assert.Equal(value, result.Output);
        }
    }

    [Fact]
    public void NumericBoundsIncludeExactTypeLimits()
    {
        var decimals = Validator.Decimal().Min(decimal.MinValue).Max(decimal.MaxValue);
        var integers = Validator.Integer().Min(long.MinValue).Max(long.MaxValue);

        Assert.Equal(decimal.MinValue, Validator.ValidateJson(decimals,
            decimal.MinValue.ToString(CultureInfo.InvariantCulture)).Output);
        Assert.Equal(decimal.MaxValue, Validator.ValidateJson(decimals,
            decimal.MaxValue.ToString(CultureInfo.InvariantCulture)).Output);
        Assert.Equal(long.MinValue, Validator.ValidateJson(integers,
            long.MinValue.ToString(CultureInfo.InvariantCulture)).Output);
        Assert.Equal(long.MaxValue, Validator.ValidateJson(integers,
            long.MaxValue.ToString(CultureInfo.InvariantCulture)).Output);
        Assert.False(Validator.ValidateJson(Validator.Decimal().Min(0m), "-0.001").IsValid);
        Assert.False(Validator.ValidateJson(Validator.Integer().Max(4), "5").IsValid);
    }

    [Fact]
    public void DerivedRulesRetainMessagesAndOrderWithoutChangingTheOriginal()
    {
        var original = Validator.String().NotBlank("Enter a value.");
        var derived = original.MaxLength(12).Matches("^P-");
        var result = Validator.ValidateJson(derived, JsonSerializer.Serialize(new string(' ', 13)));

        Assert.True(Validator.ValidateJson(original, "\"a longer ordinary value\"").IsValid);
        Assert.Equal("Enter a value.", result.Issues[0].Message);
        Assert.Collection(result.Issues,
            issue => Assert.Equal(IssueCode.NotBlank, issue.Code),
            issue => Assert.Equal(IssueCode.MaximumLength, issue.Code),
            issue => Assert.Equal(IssueCode.Pattern, issue.Code));
    }

    [Fact]
    public void CustomRulesDoNotRunDuringConstruction()
    {
        int calls = 0;
        var schema = Validator.String().Check(value =>
        {
            calls++;
            return value.StartsWith("P-", StringComparison.Ordinal);
        }, "part.prefix", "Start with P-.");

        Assert.Equal(0, calls);
        Assert.True(Validator.ValidateJson(schema, "\"P-100\"").IsValid);
        var issue = Assert.Single(Validator.ValidateJson(schema, "\"X-100\"").Issues);
        Assert.Equal("part.prefix", issue.RuleId);
        Assert.Equal("Start with P-.", issue.Message);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void CustomRuleExceptionsPropagate()
    {
        var schema = Validator.Boolean().Check(
            value => throw new InvalidOperationException("Callback failed."),
            "broken.callback", "A message.");

        Assert.Throws<InvalidOperationException>(() => Validator.ValidateJson(schema, "true"));
    }

    [Fact]
    public void InvalidPatternsFailBeforeValidation()
    {
        Assert.ThrowsAny<ArgumentException>(() => Validator.String().Matches("["));
        Assert.Throws<ArgumentException>(() => Validator.String().Matches(new string('a', 4097)));
        Assert.True(Validator.ValidateJson(Validator.String().Matches("^P-"), "\"P-100\"").IsValid);
    }
}
