using System;
using System.Xml;

namespace InnovatorSchema;

/// <summary>An Item with declared properties and attributes.</summary>
public sealed class ItemSchema : Schema
{
    private static readonly ScalarSchema<string> IdPresence = new(ScalarReaders.String);

    private readonly string? type;
    private readonly Schema? idSchema;
    private readonly Binding[] fields;

    internal ItemSchema(string? type)
        : this(type, null, Array.Empty<Binding>(), false, 1)
    {
    }

    private ItemSchema(string? type, Schema? idSchema, Binding[] fields,
        bool allowsNullish, int depth) : base(allowsNullish, depth)
    {
        this.type = type;
        this.idSchema = idSchema;
        this.fields = fields;
    }

    /// <summary>Require the Item to have a nonblank ID.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1709",
        Justification = "ID follows IOM getID naming.")]
    public ItemSchema ID()
    {
        return idSchema is null ? ID(IdPresence) : this;
    }

    /// <summary>Require an Item ID and check it against the supplied scalar schema.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1709",
        Justification = "ID follows IOM getID naming.")]
    public ItemSchema ID<T>(ScalarSchema<T> schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        if (schema.AllowsNullish)
        {
            throw new ArgumentException("An ID rule requires a nonblank value.", nameof(schema));
        }

        return ReferenceEquals(idSchema, schema) ? this :
            new ItemSchema(type, schema, fields, AllowsNullish, Depth);
    }

    /// <summary>Check the named AML attribute.</summary>
    public ItemSchema Attribute(string name, Schema schema)
    {
        if (schema is ItemSchema)
        {
            throw new ArgumentException("An attribute requires a scalar schema.", nameof(schema));
        }

        return Add(name, schema, true);
    }

    /// <summary>Check the named AML property.</summary>
    public ItemSchema Property(string name, Schema schema)
    {
        return Add(name, schema, false);
    }

    /// <summary>Allow this Item to be omitted, null or blank when used as a property.</summary>
    public override ItemSchema Nullish()
    {
        return AllowsNullish ? this : new ItemSchema(type, idSchema, fields, true, Depth);
    }

    private ItemSchema Add(string name, Schema schema, bool attribute)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(schema);

        if (name.Length > Limits.NameLength || fields.Length >= Limits.Fields ||
            schema.Depth >= Limits.SchemaDepth)
        {
            throw new ArgumentException("Item schema limit exceeded.");
        }

        try
        {
            XmlConvert.VerifyNCName(name);
        }
        catch (XmlException error)
        {
            throw new ArgumentException("Use an unqualified AML name.", nameof(name), error);
        }

        foreach (Binding field in fields)
        {
            if (field.Name == name && field.IsAttribute == attribute)
            {
                throw new ArgumentException("The field is already declared.", nameof(name));
            }
        }

        var next = new Binding[fields.Length + 1];
        fields.CopyTo(next, 0);
        next[^1] = new Binding(name, schema, attribute);
        return new ItemSchema(type, idSchema, next, AllowsNullish,
            Math.Max(Depth, schema.Depth + 1));
    }

    internal override void Validate(FieldInput input, ValidationContext context)
    {
        if (!input.TryReadNull(context, out bool isNull))
        {
            return;
        }

        if (isNull || input.Element is not XmlElement property)
        {
            FieldInput.CheckMissing(AllowsNullish, context);
            return;
        }

        if (!TryExpanded(property, context, out XmlElement? item))
        {
            return;
        }

        if (item is null)
        {
            FieldInput.CheckMissing(AllowsNullish, context);
            return;
        }

        var iom = input.Owner?.Expanded(property, item);
        var nested = new FieldInput(null, item, iom);

        if (nested.TryReadNull(context, out bool itemIsNull))
        {
            if (itemIsNull)
            {
                FieldInput.CheckMissing(AllowsNullish, context);
            }
            else
            {
                ValidateItem(item, iom, context);
            }
        }
    }

    internal void ValidateItem(XmlElement item, IomItem? iom, ValidationContext context)
    {
        if (type is not null && item.GetAttribute("type") != type)
        {
            using var scope = context.Enter("type", attribute: true);
            context.AddMessage("Expected an Item of type " + type + ".",
                "Send an item of type " + type + ".");
            return;
        }

        if (idSchema is not null)
        {
            using var scope = context.Enter("id", attribute: true);
            string? id = iom is IomItem current ? current.Id : AmlId(item);

            if (string.IsNullOrWhiteSpace(id))
            {
                context.Add("An Item ID is required.", "is required.");
            }
            else if (!ReferenceEquals(idSchema, IdPresence))
            {
                idSchema.Validate(new FieldInput(id, null, null), context);
            }
        }

        foreach (Binding field in fields)
        {
            if (!context.Step())
            {
                return;
            }

            using var scope = context.Enter(field.Name, field.IsAttribute);
            field.Validate(item, iom, context);
        }
    }

    private static string? AmlId(XmlElement item)
    {
        // IOM getID gives the attribute precedence over an equality property.
        string id = item.GetAttribute("id");

        if (id.Length != 0 || item["id", string.Empty] is not XmlElement property)
        {
            return id;
        }

        string condition = property.GetAttribute("condition");
        return condition.Length == 0 || condition.Equals("eq", StringComparison.OrdinalIgnoreCase) ?
            property.InnerText : null;
    }

    private static bool TryExpanded(
        XmlElement property, ValidationContext context, out XmlElement? item)
    {
        item = null;

        for (XmlNode? child = property.FirstChild; child is not null; child = child.NextSibling)
        {
            if (!context.Step())
            {
                return false;
            }

            if (child is XmlElement element && item is null &&
                FieldInput.IsElement(element, "Item"))
            {
                item = element;
            }
            else if (child is XmlElement ||
                (FieldInput.IsText(child) && !string.IsNullOrWhiteSpace(child.Value)))
            {
                context.Add("An expanded Item is required.", "must include one related item.");
                return false;
            }
        }

        return true;
    }

    private readonly record struct Binding(string Name, Schema Schema, bool IsAttribute)
    {
        public void Validate(XmlElement item, IomItem? iom, ValidationContext context)
        {
            if (IsAttribute)
            {
                Schema.Validate(new FieldInput(item.GetAttributeNode(Name)?.Value, null, null),
                    context);
                return;
            }

            XmlElement? property = null;

            for (XmlNode? child = item.FirstChild; child is not null; child = child.NextSibling)
            {
                if (!context.Step())
                {
                    return;
                }

                if (child is XmlElement element && FieldInput.IsElement(element, Name))
                {
                    if (property is not null)
                    {
                        context.Add("The property occurs more than once.",
                            "has more than one value. Keep just one.");
                        return;
                    }

                    property = element;
                }
            }

            Schema.Validate(new FieldInput(null, property, iom), context);
        }
    }
}
