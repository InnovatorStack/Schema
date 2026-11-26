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
    public static ScalarSchema<string> Min(this ScalarSchema<string> schema, int minimum,
        string? message = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        CheckLength(minimum);
        return schema.Check(value => value.Length >= minimum,
            FormattableString.Invariant($"must contain at least {minimum} characters."), message);
    }

    /// <summary>Allow at most this many characters.</summary>
    public static ScalarSchema<string> Max(this ScalarSchema<string> schema, int maximum,
        string? message = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        CheckLength(maximum);
        return schema.Check(value => value.Length <= maximum,
            FormattableString.Invariant($"must contain at most {maximum} characters."), message);
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
