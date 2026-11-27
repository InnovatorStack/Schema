using System;
using System.Collections.Generic;
using System.Globalization;

namespace InnovatorSchema;

/// <summary>Rules for one text value in AML.</summary>
/// <typeparam name="T">The type used by the field's rules.</typeparam>
public sealed class ScalarSchema<T> : Schema
{
    private readonly ScalarReader<T> read;
    private readonly string? readRequirement;
    private readonly Rule[] rules;
    private readonly DefaultValue? fallback;
    private readonly Func<T, T>? transform;

    internal ScalarSchema(ScalarReader<T> read, string? readRequirement = null)
        : this(read, readRequirement, Array.Empty<Rule>(), Presence.Required, null)
    {
    }

    private ScalarSchema(ScalarReader<T> read, string? readRequirement, Rule[] rules,
        Presence presence, DefaultValue? fallback, Func<T, T>? transform = null)
        : base(presence, 1)
    {
        this.read = read;
        this.readRequirement = readRequirement;
        this.rules = rules;
        this.fallback = fallback;
        this.transform = transform;
    }

    internal bool RequiresValue => FieldPresence == Presence.Required && fallback is null;
    internal bool TransformsValue => transform is not null;

    /// <summary>Allow missing and null values. Preserve empty strings on text fields.</summary>
    public override ScalarSchema<T> Optional()
    {
        return WithPresence(Presence.Optional);
    }

    /// <summary>Accept missing, null and blank values as null.</summary>
    [Obsolete("Use Optional() for missing or null fields and to preserve empty strings.")]
    public override ScalarSchema<T> Nullish()
    {
        return WithPresence(Presence.LegacyNullish);
    }

    /// <summary>Use a validated value when the field is omitted or explicitly null.</summary>
    public ScalarSchema<T> Default(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!AcceptsLiteral(value))
        {
            throw new ArgumentException("The default is invalid for this field.", nameof(value));
        }

        foreach (Rule rule in rules)
        {
            CheckDefault(rule, value);
        }

        return new ScalarSchema<T>(read, readRequirement, rules, FieldPresence, new(value),
            transform);
    }

    /// <summary>Require the parsed value to match one of the supplied choices.</summary>
    public ScalarSchema<T> OneOf(params T[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Length == 0 || values.Length > Limits.Fields)
        {
            throw new ArgumentException("Supply between 1 and 128 choices.", nameof(values));
        }

        var choices = new HashSet<T>();

        foreach (T value in values)
        {
            if (!AcceptsLiteral(value) || value is string { Length: > Limits.NameLength } ||
                !choices.Add(value))
            {
                throw new ArgumentException("Supply distinct, valid choices.", nameof(values));
            }
        }

        string requirement = "must be one of " + string.Join(", ",
            Array.ConvertAll(values, FormatChoice)) + ".";
        return WithRule(new Rule(choices.Contains, requirement, Choices: choices));
    }

    internal ScalarSchema<T> CompareChoices(IEqualityComparer<T> comparer)
    {
        if (rules.Length == 0 || rules[^1].Choices is not HashSet<T> previous)
        {
            throw new InvalidOperationException("Use Insensitive() immediately after OneOf().");
        }

        var choices = new HashSet<T>(previous, comparer);

        if (choices.Count != previous.Count)
        {
            throw new InvalidOperationException("OneOf() choices must be distinct ignoring case.");
        }

        return WithRule(rules[^1] with { Check = choices.Contains, Choices = choices },
            replaceLast: true);
    }

    internal ScalarSchema<T> WithTransform(Func<T, T> project)
    {
        return new ScalarSchema<T>(read, readRequirement, rules, FieldPresence, fallback, project);
    }

    private static string FormatChoice(T value)
    {
        return value switch
        {
            bool flag => flag ? "1" : "0",
            DateTime date => date.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture),
            string { Length: 0 } => "an empty string",
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };
    }

    private ScalarSchema<T> WithPresence(Presence presence)
    {
        if (FieldPresence == presence)
        {
            return this;
        }

        if (fallback is { Value: string { Length: 0 } } && presence != Presence.Optional)
        {
            throw new InvalidOperationException("An empty string default requires Optional().");
        }

        return new ScalarSchema<T>(read, readRequirement, rules, presence, fallback, transform);
    }

    internal ScalarSchema<T> Check(Func<T, bool> check, string requirement,
        string? customMessage = null)
    {
        return WithRule(new Rule(check, requirement, customMessage));
    }

    private ScalarSchema<T> WithRule(Rule rule, bool replaceLast = false)
    {
        int count = rules.Length + (replaceLast ? 0 : 1);

        if (count > Limits.Rules)
        {
            throw new InvalidOperationException("The field has too many rules.");
        }

        if (fallback is DefaultValue current)
        {
            CheckDefault(rule, current.Value);
        }

        var next = new Rule[count];
        rules.CopyTo(next, 0);
        next[^1] = rule;
        return new ScalarSchema<T>(read, readRequirement, next, FieldPresence, fallback, transform);
    }

    private static void CheckDefault(Rule rule, T value)
    {
        if (!rule.Check(value))
        {
            throw new ArgumentException("The default value " + rule.Requirement, nameof(value));
        }
    }

    internal override object? Read(FieldInput input, ValidationContext context, bool capture)
    {
        if (!input.TryReadText(context, out string? text))
        {
            return null;
        }

        if (text is null)
        {
            if (fallback is DefaultValue current)
            {
                return capture ? Transform(current.Value) : null;
            }

            FieldInput.CheckMissing(FieldPresence != Presence.Required, context);
            return null;
        }

        if (FieldPresence == Presence.LegacyNullish && string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return TryReadValue(text, context, out T value) && capture ? Transform(value) : null;
    }

    internal bool TryReadValue(string text, ValidationContext context, out T value)
    {
        if (!AcceptsText(text))
        {
            context.Add("A nonblank value is required.", readRequirement ?? "cannot be blank.");
            value = default!;
            return false;
        }

        string? error = read(text, out value);

        if (error is not null)
        {
            context.Add(error, readRequirement);
            return false;
        }

        bool valid = true;

        foreach (Rule rule in rules)
        {
            if (!context.Step())
            {
                return false;
            }

            if (!rule.Check(value))
            {
                context.AddRule(rule.Requirement, rule.CustomMessage);
                valid = false;
            }
        }

        return valid;
    }

    internal T Transform(T value)
    {
        return transform is null ? value : transform(value);
    }

    private bool AcceptsText(string text)
    {
        return !string.IsNullOrWhiteSpace(text) ||
            (text.Length == 0 && FieldPresence == Presence.Optional && typeof(T) == typeof(string));
    }

    private bool AcceptsLiteral(T value)
    {
        return value switch
        {
            null => false,
            string text => text.Length <= Limits.Characters && AcceptsText(text) &&
                read(text, out _) is null,
            double number => double.IsFinite(number),
            _ => true
        };
    }

    private readonly record struct Rule(Func<T, bool> Check, string Requirement,
        string? CustomMessage = null, HashSet<T>? Choices = null);

    private readonly record struct DefaultValue(T Value);
}
