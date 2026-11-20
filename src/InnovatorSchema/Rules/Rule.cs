using System;

namespace InnovatorSchema;

/// <summary>Reusable checks on present values, independent of source parsing.</summary>
public sealed class Rule<T>
{
    internal Rule(ValueRule<T> check)
    {
        Checks = new RuleSet<T>(new[] { check });
    }

    private Rule(RuleSet<T> checks)
    {
        Checks = checks;
    }

    internal RuleSet<T> Checks { get; }

    /// <summary>Run these checks before the supplied checks, preserving both definitions.</summary>
    public Rule<T> And(Rule<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new Rule<T>(Checks.Combine(other.Checks));
    }
}

internal abstract class ValueRule<T>
{
    protected ValueRule(IssueCode code, string? message)
    {
        Code = code;
        Message = DefinitionGuard.Message(message);
    }

    public IssueCode Code { get; }
    public string? Message { get; }
    public virtual string? RuleId => null;
    public abstract string DefaultMessage { get; }

    public virtual bool Check(T value, ValidationContext context)
    {
        if (IsSatisfiedBy(value))
        {
            return true;
        }

        context.Add(Code, Message ?? DefaultMessage, RuleId);
        return false;
    }

    public virtual void CheckCompatibility(ValueRule<T> other)
    {
    }

    protected abstract bool IsSatisfiedBy(T value);
}

internal sealed class RuleSet<T>
{
    private readonly ValueRule<T>[] rules;

    public RuleSet(ValueRule<T>[] ownedRules)
    {
        rules = ownedRules;
    }

    public int Count => rules.Length;

    public RuleSet<T> Combine(RuleSet<T> other)
    {
        if (Count + other.Count > DefinitionLimits.RulesPerValue)
        {
            throw new InvalidOperationException("A value-check stage may have at most 64 rules.");
        }

        foreach (ValueRule<T> first in rules)
        {
            foreach (ValueRule<T> second in other.rules)
            {
                first.CheckCompatibility(second);
                second.CheckCompatibility(first);
            }
        }

        var combined = new ValueRule<T>[Count + other.Count];
        Array.Copy(rules, combined, Count);
        Array.Copy(other.rules, 0, combined, Count, other.Count);
        return new RuleSet<T>(combined);
    }

    public bool Check(T value, ValidationContext context)
    {
        bool valid = true;

        foreach (ValueRule<T> rule in rules)
        {
            if (!context.Step())
            {
                return false;
            }

            if (!rule.Check(value, context))
            {
                valid = false;
            }
        }

        return valid;
    }
}
