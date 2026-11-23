using System;
using System.Xml;

namespace InnovatorSchema;

/// <summary>Rules for one text value in AML.</summary>
/// <typeparam name="T">The type used by the field's rules.</typeparam>
public sealed class ScalarSchema<T> : Schema
{
    private readonly ScalarReader<T> read;
    private readonly Rule[] rules;

    internal ScalarSchema(ScalarReader<T> read)
        : this(read, Array.Empty<Rule>(), false)
    {
    }

    private ScalarSchema(ScalarReader<T> read, Rule[] rules, bool allowsNullish)
        : base(allowsNullish, 1)
    {
        this.read = read;
        this.rules = rules;
    }

    /// <summary>Accept missing, null, empty and whitespace values.</summary>
    public override ScalarSchema<T> Nullish()
    {
        return AllowsNullish ? this : new ScalarSchema<T>(read, rules, true);
    }

    internal ScalarSchema<T> Check(Func<T, bool> check, string message)
    {
        if (rules.Length >= Limits.Rules)
        {
            throw new InvalidOperationException("The field has too many rules.");
        }

        var next = new Rule[rules.Length + 1];
        rules.CopyTo(next, 0);
        next[^1] = new Rule(check, message);
        return new ScalarSchema<T>(read, next, AllowsNullish);
    }

    internal override void Validate(FieldInput input, ValidationContext context)
    {
        if (!TryText(input, context, out string? text))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            FieldInput.CheckMissing(AllowsNullish, context);
            return;
        }

        string? error = read(text, out T value);

        if (error is not null)
        {
            context.Add(error);
            return;
        }

        foreach (Rule rule in rules)
        {
            if (!context.Step())
            {
                return;
            }

            if (!rule.Check(value))
            {
                context.Add(rule.Message);
            }
        }
    }

    private static bool TryText(FieldInput input, ValidationContext context, out string? text)
    {
        text = input.Text;

        if (!input.TryReadNull(context, out bool isNull))
        {
            return false;
        }

        if (!isNull && input.Element is XmlElement property)
        {
            int length = 0;

            for (XmlNode? child = property.FirstChild; child is not null; child = child.NextSibling)
            {
                if (!context.Step())
                {
                    return false;
                }

                if (child is XmlElement)
                {
                    context.Add("Text is required.");
                    return false;
                }

                int size = child.Value?.Length ?? 0;

                if (FieldInput.IsText(child) && size > Limits.Characters - length)
                {
                    context.Add("Field text limit exceeded.");
                    return false;
                }

                if (FieldInput.IsText(child))
                {
                    length += size;
                }
            }

            text = property.InnerText;
        }

        if (text?.Length > Limits.Characters)
        {
            context.Add("Field text limit exceeded.");
            return false;
        }

        return true;
    }

    private readonly record struct Rule(Func<T, bool> Check, string Message);
}
