using System;
using System.Xml;

namespace InnovatorSchema;

/// <summary>An Item with declared properties and attributes.</summary>
public sealed class ItemSchema : Schema
{
    private static readonly ScalarSchema<string> ArasId =
        new(ScalarReaders.ReadId, "must be a valid Aras ID.");

    private readonly string? type;
    private readonly Schema? idSchema;
    private readonly Binding[] fields;

    internal bool HasId => idSchema is not null;

    internal ItemSchema(string? type)
        : this(type, null, Array.Empty<Binding>(), Presence.Required, 1)
    {
    }

    private ItemSchema(string? type, Schema? idSchema, Binding[] fields,
        Presence presence, int depth, bool quotesSql = false) : base(presence, depth, quotesSql)
    {
        this.type = type;
        this.idSchema = idSchema;
        this.fields = fields;
    }

    /// <summary>Require a valid Aras ID and expose the original text when parsing.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1709",
        Justification = "ID follows IOM getID naming.")]
    public ItemSchema ID()
    {
        return idSchema is null ? ID(ArasId) : this;
    }

    /// <summary>Require an Item ID and check it against the supplied scalar schema.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1709",
        Justification = "ID follows IOM getID naming.")]
    public ItemSchema ID<T>(ScalarSchema<T> schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        if (!schema.RequiresValue)
        {
            throw new ArgumentException("An ID rule requires a nonblank value.", nameof(schema));
        }

        if (schema.QuotesSql)
        {
            throw new ArgumentException("An ID rule must preserve the ID value.", nameof(schema));
        }

        return ReferenceEquals(idSchema, schema) ? this :
            new ItemSchema(type, schema, fields, FieldPresence, Depth, QuotesSql);
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

    /// <summary>Allow this Item to be omitted or null when used as a property.</summary>
    public override ItemSchema Optional()
    {
        return WithPresence(Presence.Optional);
    }

    /// <summary>Allow this Item to be omitted, null or blank when used as a property.</summary>
    [Obsolete("Use Optional() for missing or null fields and to preserve empty strings.")]
    public override ItemSchema Nullish()
    {
        return WithPresence(Presence.LegacyNullish);
    }

    private ItemSchema WithPresence(Presence presence)
    {
        return FieldPresence == presence ? this :
            new ItemSchema(type, idSchema, fields, presence, Depth, QuotesSql);
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

        if (FindField(name, attribute) >= 0)
        {
            throw new ArgumentException("The field is already declared.", nameof(name));
        }

        var next = new Binding[fields.Length + 1];
        fields.CopyTo(next, 0);
        next[^1] = new Binding(name, schema, attribute);
        return new ItemSchema(type, idSchema, next, FieldPresence,
            Math.Max(Depth, schema.Depth + 1), QuotesSql || schema.QuotesSql);
    }

    internal int FindField(string name, bool attribute)
    {
        for (int index = 0; index < fields.Length; index++)
        {
            if (fields[index].Name == name && fields[index].IsAttribute == attribute)
            {
                return index;
            }
        }

        return -1;
    }

    internal void QuoteSqlValues(object?[] values, Func<string, string> escapeSqlValue)
    {
        for (int index = 0; index < fields.Length; index++)
        {
            if (!fields[index].Schema.QuotesSql)
            {
                continue;
            }

            if (values[index] is ParsedItem child)
            {
                child.QuoteSqlValues(escapeSqlValue);
            }
            else if (values[index] is string text)
            {
                string escaped = escapeSqlValue(text) ??
                    throw new InvalidOperationException("The SQL escaping function returned null.");
                values[index] = "N'" + escaped + "'";
            }
        }
    }

    internal override object? Read(FieldInput input, ValidationContext context, bool capture)
    {
        if (!input.TryReadNull(context, out bool isNull))
        {
            return null;
        }

        if (isNull || input.Element is not XmlElement property)
        {
            FieldInput.CheckMissing(FieldPresence != Presence.Required, context);
            return null;
        }

        if (!TryExpanded(property, context, out XmlElement? item))
        {
            return null;
        }

        if (item is null)
        {
            if (FieldPresence != Presence.LegacyNullish)
            {
                context.Add("An expanded Item is required.", "must include one related item.");
            }

            return null;
        }

        var iom = input.Owner?.Expanded(property, item);
        var nested = new FieldInput(null, item, iom);

        if (!nested.TryReadNull(context, out bool itemIsNull))
        {
            return null;
        }

        if (itemIsNull)
        {
            FieldInput.CheckMissing(FieldPresence != Presence.Required, context);
            return null;
        }

        return ReadItem(item, iom, context, capture);
    }

    internal ParsedItem? ReadItem(XmlElement item, IomItem? iom, ValidationContext context,
        bool capture)
    {
        if (type is not null && item.GetAttribute("type") != type)
        {
            using var scope = context.Enter("type", attribute: true);
            context.AddMessage("Expected an Item of type " + type + ".",
                "Send an item of type " + type + ".");
            return null;
        }

        object? id = ReadId(item, iom, context, capture);
        object?[]? values = capture ? new object?[fields.Length] : null;

        for (int index = 0; index < fields.Length; index++)
        {
            if (!context.Step())
            {
                return null;
            }

            Binding field = fields[index];
            using var scope = context.Enter(field.Name, field.IsAttribute);
            object? value = field.Read(item, iom, context, capture);

            if (values is not null)
            {
                values[index] = value;
            }
        }

        return values is null ? null : new ParsedItem(this, id, values);
    }

    private object? ReadId(XmlElement item, IomItem? iom, ValidationContext context, bool capture)
    {
        if (idSchema is null)
        {
            return null;
        }

        using var scope = context.Enter("id", attribute: true);
        string? id = iom is IomItem current ? current.Id : AmlId(item);

        if (string.IsNullOrWhiteSpace(id))
        {
            context.Add("An Item ID is required.", "is required.");
            return null;
        }

        return idSchema.Read(new FieldInput(id, null, null), context, capture);
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
        public object? Read(XmlElement item, IomItem? iom, ValidationContext context, bool capture)
        {
            if (IsAttribute)
            {
                return Schema.Read(new FieldInput(item.GetAttributeNode(Name)?.Value, null, null),
                    context, capture);
            }

            XmlElement? property = null;

            for (XmlNode? child = item.FirstChild; child is not null; child = child.NextSibling)
            {
                if (!context.Step())
                {
                    return null;
                }

                if (child is XmlElement element && FieldInput.IsElement(element, Name))
                {
                    if (property is not null)
                    {
                        context.Add("The property occurs more than once.",
                            "has more than one value. Keep just one.");
                        return null;
                    }

                    property = element;
                }
            }

            return Schema.Read(new FieldInput(null, property, iom), context, capture);
        }
    }
}
