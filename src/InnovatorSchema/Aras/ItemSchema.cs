using System;
using System.Xml;

namespace InnovatorSchema;

/// <summary>An Item shape requiring an exact type and already supplied property data.</summary>
public sealed class ItemSchema : Schema<ValidatedObject>
{
    internal ItemSchema(string itemType, ObjectSchema properties)
        : base(new ItemReader(itemType, properties))
    {
        ItemType = itemType;
        Properties = properties;
    }

    /// <summary>The exact type attribute expected on the Item.</summary>
    public string ItemType { get; }

    /// <summary>The declared fields and unknown-property policy.</summary>
    public ObjectSchema Properties { get; }

    /// <summary>Add a property to a new Item shape.</summary>
    public ItemSchema Property<T>(string name, Schema<T> schema)
    {
        return new ItemSchema(ItemType, Properties.Property(name, schema));
    }

    /// <summary>Add a field handle that can read the validated Item output.</summary>
    public ItemSchema Property(Field field)
    {
        return new ItemSchema(ItemType, Properties.Property(field));
    }

    /// <summary>Choose whether a new Item shape accepts undeclared properties.</summary>
    public ItemSchema UnknownProperties(UnknownPropertyHandling handling)
    {
        return new ItemSchema(ItemType, Properties.UnknownProperties(handling));
    }
}

internal sealed class ItemReader : ValueReader<ValidatedObject>
{
    private readonly string itemType;
    private readonly ObjectSchema properties;

    public ItemReader(string itemType, ObjectSchema properties)
        : base(properties.Graph.WithStage(1))
    {
        this.itemType = DefinitionGuard.Name(itemType, nameof(itemType));
        this.properties = properties;
        CheckPropertyNames(properties);
    }

    protected override ValidationValue<ValidatedObject> ReadValue(
        InputNode input, ValidationContext context)
    {
        input = input is ArasXmlInputNode source ? source.ExpandedItem() : input;

        if (input is not ArasXmlInputNode xml || xml.Element.Name != "Item" ||
            xml.Element.NamespaceURI.Length != 0 || !input.CanReadObject)
        {
            context.Add(IssueCode.ExpansionRequired, "An expanded Item is required.");
            return ValidationValue<ValidatedObject>.Invalid();
        }

        if (!input.Validate(context))
        {
            return ValidationValue<ValidatedObject>.Invalid();
        }

        InputNode type = input.Attribute("type");

        if (type.Kind != InputKind.String || type.Text != itemType)
        {
            using var scope = context.Enter(new PathSegment(PathSegmentKind.Attribute, "type"));
            context.Add(IssueCode.ItemType, "Expected an Item of type " + itemType + ".");
            return ValidationValue<ValidatedObject>.Invalid();
        }

        return properties.Reader.Read(input, context, PresencePolicy.Required);
    }

    private static void CheckPropertyNames(ObjectSchema properties)
    {
        foreach (Field field in properties.Fields)
        {
            if (field.Selector.Source == FieldSource.Relationship)
            {
                continue;
            }

            try
            {
                XmlConvert.VerifyNCName(field.Name);
            }
            catch (XmlException exception)
            {
                throw new ArgumentException("Item fields require unqualified XML names.",
                    nameof(properties), exception);
            }
        }
    }
}
