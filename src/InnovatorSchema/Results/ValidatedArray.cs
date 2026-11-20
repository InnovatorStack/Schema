using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace InnovatorSchema;

/// <summary>Validated elements in source order, including accepted null positions.</summary>
public sealed class ValidatedArray<T> : IReadOnlyList<ValidationValue<T>>, IParsedOutput
{
    private readonly ValidationValue<T>[] values;
    private IReadOnlyList<T>? presentValues;

    internal ValidatedArray(ValidationValue<T>[] ownedValues)
    {
        values = ownedValues;
    }

    /// <summary>The number of elements, including accepted null elements.</summary>
    public int Count => values.Length;

    /// <summary>The value and presence state at the specified input index.</summary>
    public ValidationValue<T> this[int index] => values[index];

    /// <summary>
    /// Extract present values in order; throws if any element is null or missing.
    /// </summary>
    public IReadOnlyList<T> GetValues()
    {
        IReadOnlyList<T>? existing = Volatile.Read(ref presentValues);

        if (existing is not null)
        {
            return existing;
        }

        for (int index = 0; index < values.Length; index++)
        {
            if (!values[index].HasValue)
            {
                throw new InvalidOperationException(
                    "The collection contains a missing or null value.");
            }
        }

        var view = new PresentValues(values);
        return Interlocked.CompareExchange(ref presentValues, view, null) ?? view;
    }

    /// <inheritdoc />
    public IEnumerator<ValidationValue<T>> GetEnumerator()
    {
        foreach (ValidationValue<T> value in values)
        {
            yield return value;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    object IParsedOutput.ToOutput()
    {
        var result = new object?[values.Length];

        for (int index = 0; index < values.Length; index++)
        {
            result[index] = OutputMaterializer.Read(values[index]);
        }

        return Array.AsReadOnly(result);
    }

    private sealed class PresentValues : IReadOnlyList<T>
    {
        private readonly ValidationValue<T>[] values;

        public PresentValues(ValidationValue<T>[] values)
        {
            this.values = values;
        }

        public int Count => values.Length;
        public T this[int index] => values[index].Value;

        public IEnumerator<T> GetEnumerator()
        {
            foreach (ValidationValue<T> value in values)
            {
                yield return value.Value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
