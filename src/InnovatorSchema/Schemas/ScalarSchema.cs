using System;
using System.Xml;

namespace InnovatorSchema;

/// <summary>Rules for one text value in AML.</summary>
/// <typeparam name="T">The type used by the field's rules.</typeparam>
public sealed class ScalarSchema<T> : Schema
{
    private readonly ScalarReader<T> read;
    private readonly string? readRequirement;
    private readonly Rule[] rules;

    internal ScalarSchema(ScalarReader<T> read, string? readRequirement = null)
        : this(read, readRequirement, Array.Empty<Rule>(), false)
    {
    }

    private ScalarSchema(ScalarReader<T> read, string? readRequirement, Rule[] rules,
        bool allowsNullish)
        : base(allowsNullish, 1)
    {
        this.read = read;
        this.readRequirement = readRequirement;
        this.rules = rules;
    }

    /// <summary>Accept missing, null, empty and whitespace values.</summary>
    public override ScalarSchema<T> Nullish()
    {
        return AllowsNullish ? this : new ScalarSchema<T>(read, readRequirement, rules, true);
    }

    internal ScalarSchema<T> Check(Func<T, bool> check, string requirement,
        string? customMessage = null)
    {
        if (rules.Length >= Limits.Rules)
        {
            throw new InvalidOperationException("The field has too many rules.");
        }

        var next = new Rule[rules.Length + 1];
        rules.CopyTo(next, 0);
        next[^1] = new Rule(check, requirement, customMessage);
        return new ScalarSchema<T>(read, readRequirement, next, AllowsNullish);
    }

    internal override object? Read(FieldInput input, ValidationContext context, bool capture)
    {
        if (!TryText(input, context, out string? text))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            FieldInput.CheckMissing(AllowsNullish, context);
            return null;
        }

        string? error = read(text, out T value);

        if (error is not null)
        {
            context.Add(error, readRequirement);
            return null;
        }

        foreach (Rule rule in rules)
        {
            if (!context.Step())
            {
                return null;
            }

            if (!rule.Check(value))
            {
                context.AddRule(rule.Requirement, rule.CustomMessage);
            }
        }

        return capture ? value : null;
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
                    context.Add("Text is required.", "must contain a text value.");
                    return false;
                }

                int size = child.Value?.Length ?? 0;

                if (FieldInput.IsText(child) && size > Limits.Characters - length)
                {
                    context.Add("Field text limit exceeded.", "is too long. Shorten the value.");
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
            context.Add("Field text limit exceeded.", "is too long. Shorten the value.");
            return false;
        }

        return true;
    }

    private readonly record struct Rule(Func<T, bool> Check, string Requirement,
        string? CustomMessage);
}
