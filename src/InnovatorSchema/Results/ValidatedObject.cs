using System;

namespace InnovatorSchema;

/// <summary>Read declared fields using the same handles that defined the object.</summary>
public sealed class ValidatedObject : IParsedOutput
{
    private readonly ObjectValueReader schema;
    private readonly FieldValue[] values;

    internal ValidatedObject(ObjectValueReader schema, FieldValue[] ownedValues)
    {
        this.schema = schema;
        values = ownedValues;
    }

    /// <summary>Read a present field; missing or null output throws.</summary>
    public T Get<T>(Field<T> field)
    {
        return Read(field).Value;
    }

    /// <summary>Read a field while preserving its missing, null or present state.</summary>
    public ValidationValue<T> Read<T>(Field<T> field)
    {
        ArgumentNullException.ThrowIfNull(field);

        if (schema.FieldIndexes.TryGetValue(field, out int index))
        {
            return values[index].Read<T>();
        }

        throw new ArgumentException(
            "This field handle does not belong to the object.", nameof(field));
    }

    object IParsedOutput.ToOutput()
    {
        return OutputMaterializer.Object(schema.Fields, values);
    }
}
