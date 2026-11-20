using System;
using System.Linq;
using Xunit;

namespace InnovatorSchema.Tests;

public sealed class ConstructionTests
{
    [Fact]
    public void ContradictoryBoundsFailDuringConstruction()
    {
        Assert.Throws<ArgumentException>(() => Validator.String().MinLength(5).MaxLength(4));
        Assert.Throws<ArgumentException>(() => Validator.String().MaxLength(4).MinLength(5));
        Assert.Throws<ArgumentException>(() => Validator.String().MaxLength(0).NotBlank());
        Assert.Throws<ArgumentException>(() => Validator.String().NotBlank().MaxLength(0));
        Assert.Throws<ArgumentException>(() => Validator.Decimal().Min(5m).Max(4m));
        Assert.Throws<ArgumentException>(() => Validator.Integer().Max(4).Min(5));
        Assert.Throws<ArgumentException>(() => Validator.Integer().Min(5).Min(1).Max(4));
        Assert.Throws<ArgumentException>(() => Validator.Integer().Max(4).Max(8).Min(5));
    }

    [Fact]
    public void InvalidRuleArgumentsFailDuringConstruction()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Validator.String().MinLength(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Validator.String().MaxLength(-1));
        Assert.Throws<ArgumentException>(() => Validator.String().NotBlank(" "));
        Assert.Throws<ArgumentException>(() => Validator.String().NotBlank(new string('x', 1025)));
        Assert.Throws<ArgumentException>(() =>
            Validator.Boolean().Check(value => value, " ", "Text"));
        Assert.Throws<ArgumentException>(() =>
            Validator.Boolean().Check(value => value, "id", " "));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Validator.BooleanText((BooleanTextFormat)123));
    }

    [Fact]
    public void NullArgumentsFailAtTheDeclarationBoundary()
    {
        // Exercise C# 6 callers, where nullable annotations are not enforced.
        Assert.Throws<ArgumentNullException>(() => Validator.Object(null!));
        Assert.Throws<ArgumentNullException>(() => Validator.Field<string>("name", null!));
        Assert.Throws<ArgumentNullException>(() => Validator.String().Matches(null!));
        Assert.Throws<ArgumentNullException>(() =>
            Validator.Boolean().Check(null!, "id", "Text"));
        Assert.Throws<ArgumentNullException>(() =>
            Validator.Boolean().Check(value => value, "id", null!));
        Assert.Throws<ArgumentException>(() => Validator.Object(new Field[] { null! }));
    }

    [Fact]
    public void LimitsRulesPerValue()
    {
        var schema = Validator.Integer();

        for (int index = 0; index < 64; index++)
        {
            schema = schema.Min(0);
        }

        Assert.True(Validator.ValidateJson(schema, "0").IsValid);
        Assert.Throws<InvalidOperationException>(() => schema.Max(100));
    }

    [Fact]
    public void LimitsObjectConstructionWork()
    {
        Field[] fields = Enumerable.Range(0, 256)
            .Select(index => Validator.Field("field_" + index, Validator.Boolean()))
            .ToArray();

        var schema = Validator.Object(fields);

        Assert.Equal(256, schema.Fields.Count);
        Assert.Throws<ArgumentException>(() => schema.Property("extra", Validator.Boolean()));

        var ruleHeavy = Validator.Integer();

        for (int index = 0; index < 64; index++)
        {
            ruleHeavy = ruleHeavy.Min(0);
        }

        Field[] heavyFields = Enumerable.Range(0, 64)
            .Select(index => Validator.Field("field_" + index, ruleHeavy))
            .ToArray();
        var atLimit = Validator.Object(heavyFields);

        Assert.Equal(64, atLimit.Fields.Count);
        Assert.Throws<ArgumentException>(() =>
            atLimit.Property("extra", Validator.String().NotBlank()));
    }
}
