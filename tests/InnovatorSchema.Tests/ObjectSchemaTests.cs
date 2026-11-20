using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace InnovatorSchema.Tests;

public sealed class ObjectSchemaTests
{
    [Fact]
    public void DefensivelyCopiesBindingsAndExposesAReadOnlyView()
    {
        var number = Validator.Field("item_number", Validator.String().NotBlank());
        var weight = Validator.Field("weight", Validator.DecimalText().Min(0m));
        Field[] supplied = { number, weight };
        var schema = Validator.Object(supplied);

        supplied[0] = Validator.Field("replacement", Validator.Boolean());

        Assert.Same(number, schema.Fields[0]);
        Assert.Same(weight, schema.Fields[1]);
        var mutableView = Assert.IsAssignableFrom<IList<Field>>(schema.Fields);
        Assert.Throws<NotSupportedException>(() => mutableView.Add(number));
        Assert.Throws<NotSupportedException>(() => mutableView[0] = weight);
    }

    [Fact]
    public void PropertyAndFieldDeclarationsShareTheSameModel()
    {
        var value = Validator.String().NotBlank();
        var field = Validator.Field("first", value);
        var original = Validator.Object(field);
        var derived = original.Property("second", value);

        Assert.Single(original.Fields);
        Assert.Same(field, derived.Fields[0]);
        var second = Assert.IsType<Field<string>>(derived.Fields[1]);
        Assert.Same(value, field.Schema);
        Assert.Same(value, second.Schema);
        Assert.Equal("first", field.Name);
        Assert.Equal("second", second.Name);
    }

    [Fact]
    public void RejectsDuplicatesAndRetainsExactNames()
    {
        var schema = Validator.Object().Property("name", Validator.String());

        Assert.Throws<ArgumentException>(() => schema.Property("name", Validator.Integer()));
        Assert.Throws<ArgumentException>(() =>
            Validator.Object(schema.Fields[0], schema.Fields[0]));
        Assert.Equal(2, schema.Property("Name", Validator.String()).Fields.Count);
        Assert.Equal("name", schema.Fields[0].Name);
        Assert.Throws<ArgumentException>(() => Validator.Field(" ", Validator.Boolean()));
        Assert.Throws<ArgumentException>(() =>
            Validator.Field(new string('x', 257), Validator.Boolean()));
    }

    [Fact]
    public void UnknownPropertyPoliciesAreExplicitAndImmutable()
    {
        var json = Validator.Object().Property("name", Validator.String());
        var item = Validator.Item("Part").Property("name", Validator.String());
        var strictItem = item.UnknownProperties(UnknownPropertyHandling.Reject);

        Assert.Equal(UnknownPropertyHandling.Allow, json.UnknownPropertyHandling);
        Assert.Equal(UnknownPropertyHandling.Allow, item.Properties.UnknownPropertyHandling);
        Assert.Equal(UnknownPropertyHandling.Reject, strictItem.Properties.UnknownPropertyHandling);
        Assert.Same(item.Properties.Fields[0], strictItem.Properties.Fields[0]);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            json.UnknownProperties((UnknownPropertyHandling)123));
    }

    [Fact]
    public void ItemSchemasCheckXmlPropertyNamesAtConstruction()
    {
        var json = Validator.Object().Property("a b", Validator.String());

        Assert.Equal("a b", json.Fields[0].Name);
        Assert.Throws<ArgumentException>(() => Validator.Item("Part", json.Fields[0]));
        Assert.Throws<ArgumentException>(() =>
            Validator.Item("Part").Property("@action", Validator.String()));
        Assert.Throws<ArgumentException>(() => Validator.Item(" "));
        Assert.Equal("Part BOM", Validator.Item("Part BOM").ItemType);
    }

    [Fact]
    public void SharedDefinitionsSupportConcurrentReaders()
    {
        var text = Validator.String().NotBlank().MaxLength(20).Matches("^P-");
        var first = Validator.Object().Property("first", text);
        var second = Validator.Object().Property("second", text.Optional());

        Parallel.For(0, 1000, index =>
        {
            Assert.Equal("first", first.Fields[0].Name);
            Assert.Equal("second", second.Fields[0].Name);
            Assert.True(Validator.ValidateJson(text, "\"P-100\"").IsValid);
            Assert.Equal(PresencePolicy.Required, text.InputPresence);
        });
    }
}
