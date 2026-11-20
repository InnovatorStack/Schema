using System;

namespace InnovatorSchema;

/// <summary>The presence or validity of a selected value.</summary>
public enum ValueState
{
    /// <summary>The source member was omitted.</summary>
    Missing,
    /// <summary>The selected or transformed value is null.</summary>
    Null,
    /// <summary>A present, non-null value is available.</summary>
    Present,
    /// <summary>Validation failed; reading Value throws.</summary>
    Invalid
}

/// <summary>
/// A value and its missing, null, present or invalid state.
/// </summary>
public readonly struct ValidationValue<T>
{
    private readonly T? value;

    private ValidationValue(ValueState state, T? value = default)
    {
        State = state;
        this.value = value;
    }

    /// <summary>The explicit presence or validity state.</summary>
    public ValueState State { get; }

    /// <summary>True only for the Present state.</summary>
    public bool HasValue => State == ValueState.Present;

    /// <summary>Whether the source member was omitted.</summary>
    public bool IsMissing => State == ValueState.Missing;

    /// <summary>Whether the selected or transformed value is null.</summary>
    public bool IsNull => State == ValueState.Null;

    /// <summary>The usable value; throws when missing, null or invalid.</summary>
    public T Value
    {
        get
        {
            if (HasValue && value is T present)
            {
                return present;
            }

            throw new InvalidOperationException("This value is missing, null or invalid.");
        }
    }

    internal static ValidationValue<T> Present(T value)
    {
        if (value is null)
        {
            return Null();
        }

        return new ValidationValue<T>(ValueState.Present, value);
    }

    internal static ValidationValue<T> Missing()
    {
        return new ValidationValue<T>(ValueState.Missing);
    }

    internal static ValidationValue<T> Null()
    {
        return new ValidationValue<T>(ValueState.Null);
    }

    internal static ValidationValue<T> Invalid()
    {
        return new ValidationValue<T>(ValueState.Invalid);
    }

    internal ValidationValue<TNext> WithoutValue<TNext>()
    {
        return State switch
        {
            ValueState.Missing => ValidationValue<TNext>.Missing(),
            ValueState.Null => ValidationValue<TNext>.Null(),
            _ => ValidationValue<TNext>.Invalid()
        };
    }
}
