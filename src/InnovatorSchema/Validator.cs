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

    /// <summary>Require an exact match to one of the supplied strings.</summary>
    public static ScalarSchema<string> Enum(params string[] values)
    {
        return String().Enum(values);
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

    /// <summary>Read validated values from one Item supplied as AML text or IOM.</summary>
    /// <exception cref="ValidationException">The input fails validation.</exception>
    public static dynamic ParseInput(ItemSchema schema, object? input)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return InputAssertion.Parse(schema, input);
    }

    /// <summary>Read the validated values of every Item in the supplied collection.</summary>
    /// <exception cref="ValidationException">The input fails validation.</exception>
    public static IReadOnlyList<dynamic> ParseInput(ItemArraySchema schema, object? input)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return InputAssertion.Parse(schema, input);
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
