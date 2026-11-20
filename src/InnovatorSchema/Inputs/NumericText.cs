using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace InnovatorSchema;

internal static class NumericText
{
    private static readonly BigInteger DecimalMaximum = BigInteger.Parse(
        "79228162514264337593543950335", CultureInfo.InvariantCulture);

    public static bool TryDecimal(string text, out decimal value)
    {
        value = 0m;

        if (!TryParts(text, out BigInteger coefficient, out int scale))
        {
            return false;
        }

        if (coefficient.IsZero)
        {
            return true;
        }

        // Decimal parsing can round; prove exact representability before conversion.
        if (!Normalize(ref coefficient, ref scale, 28) ||
            BigInteger.Abs(coefficient) > DecimalMaximum)
        {
            return false;
        }

        const NumberStyles styles = NumberStyles.AllowLeadingSign |
            NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;

        return decimal.TryParse(text, styles, CultureInfo.InvariantCulture, out value);
    }

    public static bool TryInteger(string text, out long value)
    {
        value = 0;

        if (!TryParts(text, out BigInteger coefficient, out int scale))
        {
            return false;
        }

        if (coefficient.IsZero)
        {
            return true;
        }

        if (!Normalize(ref coefficient, ref scale, 19) || scale != 0 ||
            coefficient < long.MinValue || coefficient > long.MaxValue)
        {
            return false;
        }

        value = checked((long)coefficient);
        return true;
    }

    private static bool Normalize(ref BigInteger coefficient, ref int scale, int maximumScale)
    {
        while (scale > 0 && coefficient % 10 == 0)
        {
            coefficient /= 10;
            scale--;
        }

        if (scale < -maximumScale || scale > maximumScale)
        {
            return false;
        }

        if (scale < 0)
        {
            coefficient *= BigInteger.Pow(10, -scale);
            scale = 0;
        }

        return true;
    }

    private static bool TryParts(string text, out BigInteger coefficient, out int scale)
    {
        coefficient = BigInteger.Zero;
        scale = 0;

        if (string.IsNullOrEmpty(text) || text.Length > 128)
        {
            return false;
        }

        int index = 0;
        bool negative = text[0] == '-';

        if (text[0] is '+' or '-')
        {
            index++;
        }

        var digits = new StringBuilder(text.Length);

        if (ReadDigits(text, ref index, digits) == 0)
        {
            return false;
        }

        if (index < text.Length && text[index] == '.')
        {
            index++;
            scale = ReadDigits(text, ref index, digits);

            if (scale == 0)
            {
                return false;
            }
        }

        if (!ReadExponent(text, ref index, out int exponent) || index != text.Length)
        {
            return false;
        }

        scale -= exponent;
        coefficient = BigInteger.Parse(
            digits.ToString(), NumberStyles.None, CultureInfo.InvariantCulture);

        if (negative)
        {
            coefficient = -coefficient;
        }

        return true;
    }

    private static int ReadDigits(string text, ref int index, StringBuilder digits)
    {
        int start = index;

        while (index < text.Length && text[index] is >= '0' and <= '9')
        {
            digits.Append(text[index]);
            index++;
        }

        return index - start;
    }

    private static bool ReadExponent(string text, ref int index, out int exponent)
    {
        exponent = 0;

        if (index == text.Length || text[index] is not ('e' or 'E'))
        {
            return true;
        }

        index++;
        bool negative = index < text.Length && text[index] == '-';

        if (index < text.Length && text[index] is '+' or '-')
        {
            index++;
        }

        int start = index;

        while (index < text.Length && text[index] is >= '0' and <= '9')
        {
            exponent = Math.Min(10_000, exponent * 10 + text[index] - '0');
            index++;
        }

        if (negative)
        {
            exponent = -exponent;
        }

        return index > start;
    }
}
