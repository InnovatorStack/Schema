using System;

namespace InnovatorSchema;

/// <summary>Apply bounds to a scalar field.</summary>
public static class ScalarRules
{
    /// <summary>Require a value at least as large as the minimum.</summary>
    public static ScalarSchema<T> Min<T>(this ScalarSchema<T> schema, T minimum,
        string? message = null) where T : struct, IComparable<T>
    {
        ArgumentNullException.ThrowIfNull(schema);
        CheckBound(minimum);
        return schema.Check(value => value.CompareTo(minimum) >= 0,
            FormattableString.Invariant($"must be at least {minimum}."), message);
    }

    /// <summary>Require a value at most as large as the maximum.</summary>
    public static ScalarSchema<T> Max<T>(this ScalarSchema<T> schema, T maximum,
        string? message = null) where T : struct, IComparable<T>
    {
        ArgumentNullException.ThrowIfNull(schema);
        CheckBound(maximum);
        return schema.Check(value => value.CompareTo(maximum) <= 0,
            FormattableString.Invariant($"must be at most {maximum}."), message);
    }

    /// <summary>Require at least this many characters.</summary>
    public static ScalarSchema<string> MinLength(this ScalarSchema<string> schema, int minimum,
        string? message = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        CheckLength(minimum);
        return schema.Check(value => value.Length >= minimum,
            FormattableString.Invariant($"must contain at least {minimum} characters."), message);
    }

    /// <summary>Allow at most this many characters.</summary>
    public static ScalarSchema<string> MaxLength(this ScalarSchema<string> schema, int maximum,
        string? message = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        CheckLength(maximum);
        return schema.Check(value => value.Length <= maximum,
            FormattableString.Invariant($"must contain at most {maximum} characters."), message);
    }

    /// <summary>Require exactly the given number of characters.</summary>
    public static ScalarSchema<string> Length(this ScalarSchema<string> schema, int length,
        string? message = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        CheckLength(length);
        return schema.Check(value => value.Length == length,
            FormattableString.Invariant($"must contain exactly {length} characters."), message);
    }

    /// <summary>Require a string length within the inclusive bounds.</summary>
    public static ScalarSchema<string> Length(this ScalarSchema<string> schema, int minimum,
        int maximum, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        CheckLength(minimum);
        CheckLength(maximum);

        if (minimum > maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(maximum),
                "Use a maximum at least the minimum.");
        }

        string requirement = FormattableString.Invariant(
            $"must contain between {minimum} and {maximum} characters.");
        return schema.Check(value => value.Length >= minimum && value.Length <= maximum,
            requirement, message);
    }

    /// <summary>Compare the preceding OneOf choices without regard to case.</summary>
    public static ScalarSchema<string> Insensitive(this ScalarSchema<string> schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return schema.CompareChoices(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Double apostrophes and optionally surround the result with single quotes.</summary>
    public static ScalarSchema<string> SqlQuoted(this ScalarSchema<string> schema,
        bool outerQuotes = true)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return schema.WithTransform(outerQuotes ?
            static value => "'" + EscapeApostrophes(value) + "'" : EscapeApostrophes);
    }

    private static string EscapeApostrophes(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }

    private static void CheckBound<T>(T value)
    {
        if (value is double number && !double.IsFinite(number))
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
    }

    private static void CheckLength(int length)
    {
        if (length < 0 || length > Limits.Characters)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }
    }
}
