using System.Globalization;
using Xunit;

namespace InnovatorSchema.Tests;

public sealed class NumericTextTests
{
    [Theory]
    [InlineData("79228162514264337593543950335", true)]
    [InlineData("79228162514264337593543950336", false)]
    [InlineData("0.0000000000000000000000000001", true)]
    [InlineData("0.00000000000000000000000000001", false)]
    [InlineData("1.00000000000000000000000000001", false)]
    [InlineData("1.00000000000000000000000000000", true)]
    [InlineData("1e28", true)]
    [InlineData("1e29", false)]
    [InlineData("-0e999999", true)]
    public void DecimalInterpretationIsExact(string text, bool expected)
    {
        Assert.Equal(expected, NumericText.TryDecimal(text, out decimal value));
    }

    [Theory]
    [InlineData("9223372036854775807", true)]
    [InlineData("9223372036854775808", false)]
    [InlineData("-9223372036854775808", true)]
    [InlineData("-9223372036854775809", false)]
    [InlineData("1.0", true)]
    [InlineData("1.5", false)]
    [InlineData("1e2", true)]
    [InlineData("1e-2", false)]
    public void IntegerInterpretationIsExact(string text, bool expected)
    {
        Assert.Equal(expected, NumericText.TryInteger(text, out long value));
    }

    [Theory]
    [InlineData(" 1")]
    [InlineData("1 ")]
    [InlineData("1,5")]
    [InlineData("1,000")]
    [InlineData(".5")]
    [InlineData("1.")]
    [InlineData("1e")]
    [InlineData("1e+")]
    [InlineData("NaN")]
    public void InvalidTextIsRejected(string text)
    {
        Assert.False(NumericText.TryDecimal(text, out decimal decimalValue));
        Assert.False(NumericText.TryInteger(text, out long integerValue));
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("tr-TR")]
    public void InterpretationIsCultureIndependent(string culture)
    {
        CultureInfo original = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);

            Assert.True(NumericText.TryDecimal("-12.50e1", out decimal value));
            Assert.Equal(-125m, value);
            Assert.True(NumericText.TryInteger("1.000e3", out long integer));
            Assert.Equal(1000, integer);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
