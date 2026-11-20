using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;

namespace InnovatorSchema;

internal readonly struct ItemXml
{
    private static readonly ConditionalWeakTable<Type, Lazy<Func<object, ItemXml>>> Readers = new();

    public ItemXml(XmlElement? node, XmlNodeList? nodes, XmlDocument? document)
    {
        Node = node;
        Nodes = nodes;
        Document = document;
    }

    public XmlElement? Node { get; }
    public XmlNodeList? Nodes { get; }
    public XmlDocument? Document { get; }

    public bool IsError
    {
        get
        {
            const string soap = "http://schemas.xmlsoap.org/soap/envelope/";
            return Document?.DocumentElement?["Body", soap]?["Fault", soap] is not null;
        }
    }

    public static ItemXml Read(object input)
    {
        if (input is XmlElement element)
        {
            return new ItemXml(element, null, element.OwnerDocument);
        }

        if (input is XmlDocument document)
        {
            return new ItemXml(document.DocumentElement, null, document);
        }

        if (input is XmlNodeList nodes)
        {
            return new ItemXml(null, nodes, null);
        }

        Type? type = input.GetType();

        for (int depth = 0; type is not null && depth < 32; depth++, type = type.BaseType)
        {
            if (type.FullName == "Aras.IOM.Item")
            {
                return Readers.GetValue(type, key =>
                    new Lazy<Func<object, ItemXml>>(() => CreateReader(key))).Value(input);
            }
        }

        throw new ArgumentException("Supply an Item, Item selection or AML XML node.",
            nameof(input));
    }

    private static Func<object, ItemXml> CreateReader(Type type)
    {
        var input = Expression.Parameter(typeof(object), "input");
        var item = Expression.Convert(input, type);
        var constructor = typeof(ItemXml).GetConstructor(new[]
        {
            typeof(XmlElement), typeof(XmlNodeList), typeof(XmlDocument)
        }) ?? throw new InvalidOperationException("The Item XML constructor is missing.");
        var result = Expression.New(constructor,
            Member(item, "node", typeof(XmlElement)),
            Member(item, "nodeList", typeof(XmlNodeList)),
            Member(item, "dom", typeof(XmlDocument)));

        return Expression.Lambda<Func<object, ItemXml>>(result, input).Compile();
    }

    private static UnaryExpression Member(Expression item, string name, Type expected)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        Expression member;

        if (item.Type.GetField(name, flags) is FieldInfo field)
        {
            member = Expression.Field(item, field);
        }
        else if (item.Type.GetProperty(name, flags) is PropertyInfo property &&
            property.GetMethod is not null && property.GetIndexParameters().Length == 0)
        {
            member = Expression.Property(item, property);
        }
        else
        {
            throw new ArgumentException("The supplied Item has no public XML member: " + name);
        }

        if (!expected.IsAssignableFrom(member.Type))
        {
            throw new ArgumentException("The Item XML member has an unsupported type: " + name);
        }

        return Expression.Convert(member, expected);
    }
}
