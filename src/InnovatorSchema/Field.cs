using System;

namespace InnovatorSchema;

/// <summary>A field handle identifying one declared source binding.</summary>
public abstract class Field
{
    internal Field(string name, FieldSelector? selector = null)
    {
        Name = DefinitionGuard.Name(name, nameof(name));
        Selector = selector ?? new FieldSelector(FieldSource.Property, name);
    }

    /// <summary>The exact, case-sensitive name used by this binding.</summary>
    public string Name { get; }

    internal abstract DefinitionGraph Definition { get; }

    internal FieldSelector Selector { get; }

    internal abstract FieldValue Read(InputNode input, ValidationContext context);
}

/// <summary>A field handle retaining the output type of its schema.</summary>
public sealed class Field<T> : Field
{
    internal Field(string name, Schema<T> schema, FieldSelector? selector = null)
        : base(name, selector)
    {
        ArgumentNullException.ThrowIfNull(schema);
        Schema = schema;
    }

    /// <summary>The schema used to read this field.</summary>
    public Schema<T> Schema { get; }

    internal override DefinitionGraph Definition => Schema.Graph;

    internal override FieldValue Read(InputNode input, ValidationContext context)
    {
        using var scope = context.Enter(Selector.Segment);

        if (!scope.IsEntered)
        {
            return FieldValue.From(ValidationValue<T>.Invalid());
        }

        return FieldValue.From(Schema.Read(Selector.Select(input), context));
    }
}
