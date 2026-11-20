using System;
using System.Text.RegularExpressions;

namespace InnovatorSchema;

internal sealed class StringValueReader : ValueReader<string>
{
    public StringValueReader() : base(DefinitionGraph.Leaf)
    {
    }

    protected override ValidationValue<string> ReadValue(InputNode input, ValidationContext context)
    {
        if (input.Kind != InputKind.String)
        {
            context.Add(IssueCode.TypeMismatch, "A string is required.");
            return ValidationValue<string>.Invalid();
        }

        return ValidationValue<string>.Present(input.Text);
    }
}

internal sealed class PatternRule : ValueRule<string>
{
    private readonly Regex regex;

    public PatternRule(string pattern, string? message) : base(IssueCode.Pattern, message)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        if (pattern.Length > DefinitionLimits.PatternLength)
        {
            throw new ArgumentException(
                "A pattern may have at most 4096 characters.", nameof(pattern));
        }

        regex = new Regex(pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(25));
    }

    public override string DefaultMessage => "Value does not match the required pattern.";

    public override bool Check(string value, ValidationContext context)
    {
        try
        {
            return base.Check(value, context);
        }
        catch (RegexMatchTimeoutException)
        {
            // Only this built-in regex owns the deadline; caller exceptions must propagate.
            context.Limit("Pattern matching exceeded its time limit.");
            return false;
        }
    }

    protected override bool IsSatisfiedBy(string value)
    {
        return regex.IsMatch(value);
    }
}
