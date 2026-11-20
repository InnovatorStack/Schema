using System;
using System.Collections.Generic;
using System.Linq;

namespace InnovatorSchema;

/// <summary>An object shape edited before pipeline checks or transforms.</summary>
public sealed class ObjectSchema : Schema<ValidatedObject>
{
    internal ObjectSchema(Field[] fields, UnknownPropertyHandling unknownProperties)
        : base(ObjectValueReader.Create(fields, unknownProperties))
    {
    }

    /// <summary>The field handles in declaration order.</summary>
    public IReadOnlyList<Field> Fields => ((ObjectValueReader)Reader).Fields;

    /// <summary>Whether undeclared source properties are accepted.</summary>
    public UnknownPropertyHandling UnknownPropertyHandling =>
        ((ObjectValueReader)Reader).UnknownPropertyHandling;

    /// <summary>Add a property to a new shape; the existing shape remains unchanged.</summary>
    public ObjectSchema Property<T>(string name, Schema<T> schema)
    {
        return Property(new Field<T>(name, schema));
    }

    /// <summary>Add a field handle that can later read its validated output.</summary>
    public ObjectSchema Property(Field field)
    {
        ArgumentNullException.ThrowIfNull(field);
        var updated = new Field[Fields.Count + 1];

        for (int index = 0; index < Fields.Count; index++)
        {
            updated[index] = Fields[index];
        }

        updated[Fields.Count] = field;
        return new ObjectSchema(updated, UnknownPropertyHandling);
    }

    /// <summary>Choose how a new shape treats undeclared source properties.</summary>
    public ObjectSchema UnknownProperties(UnknownPropertyHandling handling)
    {
        return new ObjectSchema(Fields.ToArray(), handling);
    }
}

internal sealed class ObjectValueReader : ValueReader<ValidatedObject>
{
    private ObjectValueReader(Field[] ownedFields, UnknownPropertyHandling unknownProperties)
        : base(new DefinitionGraph(ownedFields.Select(field => field.Definition)))
    {
        Fields = Array.AsReadOnly(ownedFields);
        UnknownPropertyHandling = unknownProperties;
        var indexes = new Dictionary<Field, int>();
        var declared = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 0; index < ownedFields.Length; index++)
        {
            Field field = ownedFields[index];
            indexes.Add(field, index);

            if (field.Selector.Source == FieldSource.Property)
            {
                declared.Add(field.Name);
            }
            else if (field.Selector.Source == FieldSource.Relationship)
            {
                declared.Add("Relationships");
            }
        }

        FieldIndexes = indexes;
        DeclaredProperties = declared;
    }

    public IReadOnlyList<Field> Fields { get; }
    public UnknownPropertyHandling UnknownPropertyHandling { get; }
    public IReadOnlyDictionary<Field, int> FieldIndexes { get; }
    public IReadOnlySet<string> DeclaredProperties { get; }

    public static ObjectValueReader Create(Field[] fields, UnknownPropertyHandling handling)
    {
        ArgumentNullException.ThrowIfNull(fields);

        if (fields.Length > DefinitionLimits.Fields)
        {
            throw new ArgumentException(
                "An object may declare at most 256 fields.", nameof(fields));
        }

        if (handling != UnknownPropertyHandling.Allow && handling != UnknownPropertyHandling.Reject)
        {
            throw new ArgumentOutOfRangeException(nameof(handling));
        }

        var copy = new Field[fields.Length];
        Array.Copy(fields, copy, fields.Length);
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (Field field in copy)
        {
            if (field is null || !names.Add(field.Selector.Key))
            {
                throw new ArgumentException("Fields must be non-null and unique.", nameof(fields));
            }
        }

        CheckOutputNames(copy);

        return new ObjectValueReader(copy, handling);
    }

    private static void CheckOutputNames(Field[] fields)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        bool relationships = false;

        foreach (Field field in fields)
        {
            if (field.Selector.Source == FieldSource.Relationship)
            {
                relationships = true;
            }
            else if (!names.Add(field.Selector.OutputName))
            {
                throw new ArgumentException("Parsed field names must be unique.", nameof(fields));
            }
        }

        if (relationships && names.Contains("Relationships"))
        {
            throw new ArgumentException(
                "Relationships is reserved when relationship fields are declared.", nameof(fields));
        }
    }

    protected override ValidationValue<ValidatedObject> ReadValue(
        InputNode input, ValidationContext context)
    {
        return ObjectReader.Read(this, input, context);
    }
}

/// <summary>How an object schema treats undeclared properties without modifying input.</summary>
public enum UnknownPropertyHandling
{
    /// <summary>Report an issue for an undeclared property.</summary>
    Reject,

    /// <summary>Permit undeclared properties.</summary>
    Allow
}
