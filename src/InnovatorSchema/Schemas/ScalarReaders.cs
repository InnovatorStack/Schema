using System;
using System.Globalization;

namespace InnovatorSchema;

internal delegate string? ScalarReader<T>(string text, out T value);

internal static class ScalarReaders
{
    private static readonly string[] DateFormats = { "yyyy-MM-dd", "yyyy-MM-dd'T'HH:mm:ss" };

    public static string? String(string text, out string value)
    {
        value = text;
        return null;
    }

    public static string? Bool(string text, out bool value)
    {
        value = text == "1";
        return value || text == "0" ? null : "Expected 0 or 1.";
    }

    public static string? Integer(string text, out long value)
    {
        return long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture,
            out value) ? null : "Expected a signed 64-bit integer.";
    }

    public static string? Double(string text, out double value)
    {
        const NumberStyles style = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint |
            NumberStyles.AllowExponent;

        return double.TryParse(text, style, CultureInfo.InvariantCulture, out value) &&
            double.IsFinite(value) ? null : "Expected a finite number in AML format.";
    }

    public static string? Date(string text, out DateTime value)
    {
        return DateTime.TryParseExact(text, DateFormats, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out value) ? null : "Expected yyyy-MM-dd or yyyy-MM-ddTHH:mm:ss.";
    }

    public static string? ReadId(string text, out string value)
    {
        value = text;
        return text.Length == 32 && Guid.TryParseExact(text, "N", out Guid id) &&
            id != Guid.Empty ? null : "Expected a nonzero, 32-character Aras GUID.";
    }
}
