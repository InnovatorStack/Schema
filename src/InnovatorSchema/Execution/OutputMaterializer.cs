using System;
using System.Collections.Generic;
using System.Threading;

namespace InnovatorSchema;

internal interface IParsedOutput
{
    object ToOutput();
}

internal static class OutputMaterializer
{
    public static object? Read<T>(ValidationValue<T> value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return value.Value is IParsedOutput output ? output.ToOutput() : value.Value;
    }

    public static SafeParseResult Parse<T>(ValidationResult<T> result,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        object? output = result.IsValid ? Read(result.OutputValue) : null;
        cancellationToken.ThrowIfCancellationRequested();
        return new SafeParseResult(result, output, result.OutputValue.State);
    }

    public static object? Require(SafeParseResult result)
    {
        if (result.Error is ValidationError error)
        {
            throw new ValidationException(error);
        }

        return result.Data;
    }

    public static ParsedObject Object(IReadOnlyList<Field> fields, FieldValue[] fieldsValues)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        HashSet<string>? missing = null;
        Dictionary<string, object?>? relationships = null;

        for (int index = 0; index < fields.Count; index++)
        {
            Field field = fields[index];
            FieldValue value = fieldsValues[index];

            if (field.Selector.Source == FieldSource.Relationship)
            {
                relationships ??= new Dictionary<string, object?>(StringComparer.Ordinal);
                relationships.Add(field.Name, value.ToOutput());
            }
            else if (value.State == ValueState.Missing)
            {
                missing ??= new HashSet<string>(StringComparer.Ordinal);
                missing.Add(field.Selector.OutputName);
            }
            else
            {
                values.Add(field.Selector.OutputName, value.ToOutput());
            }
        }

        if (relationships is not null)
        {
            values.Add("Relationships", new ParsedObject(relationships));
        }

        return new ParsedObject(values, missing);
    }
}
