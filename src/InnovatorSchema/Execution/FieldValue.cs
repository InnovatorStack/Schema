using System;

namespace InnovatorSchema;

internal readonly struct FieldValue
{
    private readonly object? value;

    private FieldValue(ValueState state, object? value)
    {
        State = state;
        this.value = value;
    }

    public ValueState State { get; }

    public static FieldValue From<T>(ValidationValue<T> value)
    {
        return new FieldValue(value.State, value.HasValue ? value.Value : null);
    }

    public ValidationValue<T> Read<T>()
    {
        return State switch
        {
            ValueState.Missing => ValidationValue<T>.Missing(),
            ValueState.Null => ValidationValue<T>.Null(),
            ValueState.Invalid => ValidationValue<T>.Invalid(),
            ValueState.Present when value is T present => ValidationValue<T>.Present(present),
            _ => throw new InvalidOperationException("The field's output type is inconsistent.")
        };
    }

    public object? ToOutput()
    {
        return value is IParsedOutput output ? output.ToOutput() : value;
    }
}
