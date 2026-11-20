using System;
using System.Globalization;

namespace InnovatorSchema;

internal sealed class BoundRule<T> : ValueRule<T> where T : struct, IComparable<T>
{
    private readonly T limit;

    public BoundRule(IssueCode code, T limit, string? message) : base(code, message)
    {
        this.limit = limit;
    }

    public override string DefaultMessage
    {
        get
        {
            string text = Convert.ToString(limit, CultureInfo.InvariantCulture) ?? string.Empty;
            return Code == IssueCode.Minimum ?
                "Value must be at least " + text + "." : "Value must be at most " + text + ".";
        }
    }

    protected override bool IsSatisfiedBy(T value)
    {
        int comparison = value.CompareTo(limit);
        return Code == IssueCode.Minimum ? comparison >= 0 : comparison <= 0;
    }

    public override void CheckCompatibility(ValueRule<T> other)
    {
        if (other is BoundRule<T> bound && Code == IssueCode.Minimum &&
            bound.Code == IssueCode.Maximum && limit.CompareTo(bound.limit) > 0)
        {
            throw new ArgumentException("Minimum exceeds maximum.");
        }
    }
}

internal sealed class LengthRule : ValueRule<string>
{
    private readonly int length;

    public LengthRule(IssueCode code, int length, string? message) : base(code, message)
    {
        DefinitionGuard.NonNegative(length, nameof(length));
        this.length = length;
    }

    public override string DefaultMessage
    {
        get
        {
            string text = length.ToString(CultureInfo.InvariantCulture);
            return Code == IssueCode.MinimumLength ?
                "Length must be at least " + text + "." : "Length must be at most " + text + ".";
        }
    }

    protected override bool IsSatisfiedBy(string value)
    {
        return Code == IssueCode.MinimumLength ? value.Length >= length : value.Length <= length;
    }

    public override void CheckCompatibility(ValueRule<string> other)
    {
        if (Code == IssueCode.MaximumLength && length == 0 && other is NotBlankRule)
        {
            throw new ArgumentException("A nonblank string cannot have maximum length zero.");
        }

        if (other is LengthRule bound && Code == IssueCode.MinimumLength &&
            bound.Code == IssueCode.MaximumLength && length > bound.length)
        {
            throw new ArgumentException("Minimum length exceeds maximum length.");
        }
    }
}

internal sealed class NotBlankRule : ValueRule<string>
{
    public NotBlankRule(string? message) : base(IssueCode.NotBlank, message)
    {
    }

    public override string DefaultMessage => "A nonblank value is required.";

    protected override bool IsSatisfiedBy(string value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }
}

internal sealed class CountRule<T> : ValueRule<ValidatedArray<T>>
{
    private readonly int count;

    public CountRule(IssueCode code, int count, string? message) : base(code, message)
    {
        DefinitionGuard.NonNegative(count, nameof(count));
        this.count = count;
    }

    public override string DefaultMessage
    {
        get
        {
            string text = count.ToString(CultureInfo.InvariantCulture);
            return Code == IssueCode.MinimumCount ?
                "Collection must contain at least " + text + " entries." :
                "Collection must contain at most " + text + " entries.";
        }
    }

    protected override bool IsSatisfiedBy(ValidatedArray<T> value)
    {
        return Code == IssueCode.MinimumCount ? value.Count >= count : value.Count <= count;
    }

    public override void CheckCompatibility(ValueRule<ValidatedArray<T>> other)
    {
        if (other is CountRule<T> bound && Code == IssueCode.MinimumCount &&
            bound.Code == IssueCode.MaximumCount && count > bound.count)
        {
            throw new ArgumentException("Minimum count exceeds maximum count.");
        }
    }
}

internal sealed class CustomRule<T> : ValueRule<T>
{
    private readonly Func<T, bool> predicate;

    public CustomRule(Func<T, bool> predicate, string ruleId, string message)
        : base(IssueCode.Custom, message)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(message);
        RuleId = DefinitionGuard.Text(ruleId, nameof(ruleId), DefinitionLimits.RuleIdLength);
        this.predicate = predicate;
    }

    public override string RuleId { get; }
    public override string DefaultMessage => "The custom rule failed.";

    protected override bool IsSatisfiedBy(T value)
    {
        return predicate(value);
    }
}
