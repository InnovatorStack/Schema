using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace InnovatorSchema;

/// <summary>Declare and assert the inputs a Server Method requires.</summary>
[SuppressMessage("Naming", "CA1720", Justification = "Factories name AML value types.")]
public static class Validator
{
    /// <summary>Require an Item of the given type.</summary>
    public static ItemSchema Item(string itemType)
    {
        ArgumentNullException.ThrowIfNull(itemType);

        if (string.IsNullOrWhiteSpace(itemType) || itemType.Length > Limits.NameLength)
        {
            throw new ArgumentException("Supply a nonblank Item type.", nameof(itemType));
        }

        return new ItemSchema(itemType);
    }

    /// <summary>Require an Item of any type.</summary>
    public static ItemSchema Item()
    {
        return new ItemSchema(null);
    }

    /// <summary>Require every supplied Item to satisfy the same schema.</summary>
    public static ItemArraySchema Array(ItemSchema item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new ItemArraySchema(item);
    }

    /// <summary>Require nonblank AML text.</summary>
    public static ScalarSchema<string> String()
    {
        return new ScalarSchema<string>(ScalarReaders.String);
    }

    /// <summary>Read Aras boolean text. Zero is false and one is true.</summary>
    public static ScalarSchema<bool> Bool()
    {
        return new ScalarSchema<bool>(ScalarReaders.Bool, "must be 0 or 1.");
    }

    /// <summary>Require a signed 64-bit integer in AML text.</summary>
    public static ScalarSchema<long> Integer()
    {
        return new ScalarSchema<long>(ScalarReaders.Integer, "must be a whole number.");
    }

    /// <summary>Require a finite floating-point number in AML text.</summary>
    public static ScalarSchema<double> Double()
    {
        return new ScalarSchema<double>(ScalarReaders.Double,
            "must be a number. Use a period for decimals.");
    }

    /// <summary>Require an AML date or date and time.</summary>
    public static ScalarSchema<DateTime> Date()
    {
        return new ScalarSchema<DateTime>(ScalarReaders.Date,
            "must be a valid date, such as 2026-01-15.");
    }

    /// <summary>Require a nonzero, 32-character Aras GUID.</summary>
    public static ScalarSchema<Guid> Guid()
    {
        return new ScalarSchema<Guid>(ScalarReaders.ReadGuid, "must be a valid Aras ID.");
    }

    /// <summary>Read one Item and its Innovator instance, if supplied as IOM.</summary>
    /// <param name="schema">The schema for the Item.</param>
    /// <param name="input">An IOM Item or AML text.</param>
    /// <param name="escapeSqlValue">Aras QuoteValue for fields declared with SqlQuoted.</param>
    /// <exception cref="ValidationException">The input fails validation.</exception>
    public static (dynamic Input, dynamic? Innovator) ParseInput(ItemSchema schema, object? input,
        Func<string, string>? escapeSqlValue = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return InputAssertion.Parse(schema, input, escapeSqlValue);
    }

    /// <summary>Read a collection and its Innovator instance, if supplied as IOM.</summary>
    /// <param name="schema">The schema for the Items.</param>
    /// <param name="input">An IOM Item or AML text.</param>
    /// <param name="escapeSqlValue">Aras QuoteValue for fields declared with SqlQuoted.</param>
    /// <exception cref="ValidationException">The input fails validation.</exception>
    public static (IReadOnlyList<dynamic> Input, dynamic? Innovator) ParseInput(
        ItemArraySchema schema, object? input, Func<string, string>? escapeSqlValue = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return InputAssertion.Parse(schema, input, escapeSqlValue);
    }

    /// <summary>Assert exactly one Item supplied as AML text or an IOM Item.</summary>
    /// <exception cref="ValidationException">The input fails validation.</exception>
    public static void AssertInput(ItemSchema schema, object? input)
    {
        ArgumentNullException.ThrowIfNull(schema);
        InputAssertion.Assert(schema, null, input);
    }

    /// <summary>Assert the complete collection supplied as AML text or an IOM Item.</summary>
    /// <exception cref="ValidationException">The input fails validation.</exception>
    public static void AssertInput(ItemArraySchema schema, object? input)
    {
        ArgumentNullException.ThrowIfNull(schema);
        InputAssertion.Assert(schema.Item, schema, input);
    }
}
