using System;
using System.Collections.Generic;
using System.Dynamic;

namespace InnovatorSchema;

/// <summary>The validated values declared by an Item schema.</summary>
public sealed class ParsedItem : DynamicObject
{
    private readonly ItemSchema schema;
    private readonly object? id;
    private readonly object?[] values;

    internal ParsedItem(ItemSchema schema, object? id, object?[] values)
    {
        this.schema = schema;
        this.id = id;
        this.values = values;
    }

    /// <summary>Read a declared value by name. Prefix attribute names with @.</summary>
    public dynamic? this[string name]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(name);
            return TryRead(name, out object? value) ? value :
                throw new KeyNotFoundException($"The schema does not declare '{name}'.");
        }
    }

    /// <inheritdoc />
    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        ArgumentNullException.ThrowIfNull(binder);
        return TryRead(binder.Name, out result);
    }

    private bool TryRead(string name, out object? value)
    {
        bool attribute = name.StartsWith('@');
        int index = schema.FindField(attribute ? name[1..] : name, attribute);

        if (index >= 0)
        {
            value = values[index];
            return true;
        }

        if (schema.HasId && (name is "id" or "@id"))
        {
            value = id;
            return true;
        }

        value = null;
        return false;
    }
}
