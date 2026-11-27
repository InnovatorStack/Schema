using System;
using System.Collections.Generic;

namespace InnovatorSchema;

/// <summary>Read delimited AML text as an array of scalar values.</summary>
/// <typeparam name="T">The parsed element type.</typeparam>
public sealed class ArraySchema<T> : Schema
{
    private readonly ScalarSchema<T> element;
    private readonly string delimiter;
    private readonly CollectionBounds bounds;

    internal ArraySchema(ScalarSchema<T> element, string delimiter)
        : this(element, delimiter, CollectionBounds.Default, Presence.Required)
    {
    }

    private ArraySchema(ScalarSchema<T> element, string delimiter, CollectionBounds bounds,
        Presence presence) : base(presence, element.Depth + 1)
    {
        this.element = element;
        this.delimiter = delimiter;
        this.bounds = bounds;
    }

    /// <summary>Require at least this many elements.</summary>
    public ArraySchema<T> Min(int count)
    {
        return new ArraySchema<T>(element, delimiter, bounds.Min(count), FieldPresence);
    }

    /// <summary>Allow at most this many elements.</summary>
    public ArraySchema<T> Max(int count)
    {
        return new ArraySchema<T>(element, delimiter, bounds.Max(count), FieldPresence);
    }

    /// <summary>Allow the entire field to be omitted or null.</summary>
    public override ArraySchema<T> Optional()
    {
        return WithPresence(Presence.Optional);
    }

    /// <summary>Accept missing, null and blank values as null.</summary>
    [Obsolete("Use Optional() for missing or null fields and to preserve empty strings.")]
    public override ArraySchema<T> Nullish()
    {
        return WithPresence(Presence.LegacyNullish);
    }

    private ArraySchema<T> WithPresence(Presence presence)
    {
        return FieldPresence == presence ? this :
            new ArraySchema<T>(element, delimiter, bounds, presence);
    }

    internal override object? Read(FieldInput input, ValidationContext context, bool capture)
    {
        if (!input.TryReadText(context, out string? text))
        {
            return null;
        }

        if (text is null || (FieldPresence == Presence.LegacyNullish &&
            string.IsNullOrWhiteSpace(text)))
        {
            FieldInput.CheckMissing(FieldPresence != Presence.Required, context);
            return null;
        }

        if (text.Length == 0)
        {
            bounds.Validate(0, context, "values");
            return capture ? Array.Empty<T>() : null;
        }

        return ReadElements(text, context, capture);
    }

    private T[]? ReadElements(string text, ValidationContext context, bool capture)
    {
        var values = capture ? new List<T>() : null;
        int start = 0;

        for (int index = 0; index < Limits.Items; index++)
        {
            if (!context.Step())
            {
                return null;
            }

            int separator = text.IndexOf(delimiter, start, StringComparison.Ordinal);
            int end = separator < 0 ? text.Length : separator;

            using (context.Enter(index))
            {
                if (element.TryReadValue(text[start..end], context, out T value) &&
                    values is not null)
                {
                    values.Add(element.Transform(value));
                }
            }

            if (separator < 0)
            {
                bounds.Validate(index + 1, context, "values");
                return values?.ToArray();
            }

            start = separator + delimiter.Length;
        }

        context.AddMessage("Delimited field exceeds the element limit.",
            "Send fewer values in this field.", includeField: true);
        return null;
    }
}
