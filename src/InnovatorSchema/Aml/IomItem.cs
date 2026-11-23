using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;

namespace InnovatorSchema;

internal readonly struct IomItem
{
    private static readonly ConditionalWeakTable<Type, Lazy<Accessors>> Cache = new();

    private readonly object item;
    private readonly Accessors readers;

    private IomItem(object item, Accessors readers)
    {
        this.item = item;
        this.readers = readers;
    }

    public XmlElement? Element => readers.Element(item);
    public string? Id => readers.Id(item);
    public int Count => readers.Count(item);

    public IomItem At(int index)
    {
        return new IomItem(readers.At(item, index), readers);
    }

    public IomItem? Expanded(XmlElement property, XmlElement expected)
    {
        object? child = readers.Property(item, property.Name);

        if (child is not null && ReferenceEquals(readers.Element(child), expected))
        {
            return new IomItem(child, readers);
        }

        return null;
    }

    public static IomItem Read(object value)
    {
        Type? type = value.GetType();

        for (int depth = 0; type is not null && depth < 32; depth++, type = type.BaseType)
        {
            if (type.FullName == "Aras.IOM.Item")
            {
                var readers = Cache.GetValue(type, key => new Lazy<Accessors>(() => Bind(key)));
                return new IomItem(value, readers.Value);
            }
        }

        throw new ArgumentException("Supply AML text or an Aras IOM Item.", nameof(value));
    }

    private static Accessors Bind(Type type)
    {
        var item = Expression.Parameter(typeof(object), "item");
        var index = Expression.Parameter(typeof(int), "index");
        var name = Expression.Parameter(typeof(string), "name");
        var target = Expression.Convert(item, type);
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        Expression node = type.GetField("node", flags) is FieldInfo field ?
            Expression.Field(target, field) : Expression.Property(target, "node");

        return new Accessors(
            Expression.Lambda<Func<object, XmlElement?>>(
                Expression.Convert(node, typeof(XmlElement)), item).Compile(),
            Expression.Lambda<Func<object, string?>>(
                Call(target, "getID"), item).Compile(),
            Expression.Lambda<Func<object, int>>(
                Call(target, "getItemCount"), item).Compile(),
            Expression.Lambda<Func<object, int, object>>(
                Expression.Convert(Call(target, "getItemByIndex", index), typeof(object)),
                item, index).Compile(),
            Expression.Lambda<Func<object, string, object?>>(
                Expression.Convert(Call(target, "getPropertyItem", name), typeof(object)),
                item, name).Compile());
    }

    private static MethodCallExpression Call(
        Expression target, string method, params Expression[] arguments)
    {
        return Expression.Call(target, method, Type.EmptyTypes, arguments);
    }

    private sealed record Accessors(
        Func<object, XmlElement?> Element,
        Func<object, string?> Id,
        Func<object, int> Count,
        Func<object, int, object> At,
        Func<object, string, object?> Property);
}
