using System;
using System.Text.RegularExpressions;
using Xunit;
using V = InnovatorSchema.Validator;

namespace InnovatorSchema.Tests;

public sealed class PipelineContractTests
{
    [Fact]
    public void TheSameRulesApplyToNativeAndTextReaders()
    {
        var rule = V.Min(1m).And(V.Max(10m));

        Assert.Equal(2.5m, V.ValidateJson(V.Decimal().Check(rule), "2.5").Output);
        Assert.Equal(2.5m, V.ValidateJson(V.DecimalText().Check(rule), "\"2.5\"").Output);
        Assert.False(V.ValidateJson(V.Decimal().Check(rule), "\"2.5\"").IsValid);
        Assert.False(V.ValidateJson(V.DecimalText().Check(rule), "2.5").IsValid);
        Assert.Equal(IssueCode.Minimum,
            V.ValidateJson(V.DecimalText().Check(rule), "\"0\"").Issues[0].Code);
        Assert.Throws<ArgumentException>(() => V.Min(10m).And(V.Max(1m)));
    }

    [Fact]
    public void OptionalCollectionsRetainCountRules()
    {
        var items = V.Field("items", V.Array(V.String()).Optional().MinCount(1).MaxCount(2));
        var schema = V.Object(items);

        Assert.True(V.ValidateJson(schema, "{}").Output.Read(items).IsMissing);
        Assert.Equal(IssueCode.MinimumCount,
            V.ValidateJson(schema, "{\"items\":[]}").Issues[0].Code);
        Assert.Equal(IssueCode.MaximumCount,
            V.ValidateJson(schema, "{\"items\":[\"a\",\"b\",\"c\"]}").Issues[0].Code);
        Assert.Equal("a", V.ValidateJson(schema, "{\"items\":[\"a\"]}")
            .Output.Get(items)[0].Value);
    }

    [Fact]
    public void RulesCheckTheTransformedValue()
    {
        var normalized = V.String().Transform(value => value.Trim()).NotBlank().MaxLength(3);
        Assert.Equal("abc", V.ValidateJson(normalized, "\"  abc  \"").Output);
        Assert.Equal(IssueCode.NotBlank, V.ValidateJson(normalized, "\"   \"").Issues[0].Code);

        var differentBounds = V.Integer().Min(10).Transform(value => 0).Max(5);
        Assert.Equal(0, V.ValidateJson(differentBounds, "10").Output);
    }

    [Fact]
    public void NullableTransformsDeclareOutputPresenceSeparately()
    {
        var text = V.String().TransformNullable<string>(value => value.Length == 0 ? null : value);
        var number = V.Integer().TransformNullable<decimal>(value =>
            value > 0 ? (decimal?)value : null);

        Assert.Equal(PresencePolicy.Required, text.InputPresence);
        Assert.Equal(PresencePolicy.AllowNull, text.OutputPresence);
        Assert.True(V.ValidateJson(text, "\"\"").OutputValue.IsNull);
        Assert.False(V.ValidateJson(text, "\"\"").HasOutput);
        Assert.Equal("abc", V.ValidateJson(text, "\"abc\"").Output);
        Assert.Equal(PresencePolicy.AllowNull, number.OutputPresence);
        Assert.True(V.ValidateJson(number, "0").OutputValue.IsNull);
        Assert.Equal(2m, V.ValidateJson(number, "2").Output);
    }

    [Fact]
    public void SourcePresenceIsIndependentOfWhereItIsConfigured()
    {
        int calls = 0;
        var schema = V.String().Transform(value =>
        {
            calls++;
            return value.Length;
        }).Nullable().Optional().Min(1);
        var field = V.Field("value", schema);
        var root = V.Object(field);

        Assert.True(V.ValidateJson(root, "{}").Output.Read(field).IsMissing);
        Assert.True(V.ValidateJson(root, "{\"value\":null}").Output.Read(field).IsNull);
        Assert.Equal(0, calls);
        Assert.Equal(3, V.ValidateJson(root, "{\"value\":\"abc\"}").Output.Get(field));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void RequiredOutputChecksRunOnAcceptedNullAndMissingStates()
    {
        var schema = V.String().Optional().Nullable().Transform(value => value.Length)
            .RequireValue();
        var field = V.Field("value", schema);
        var root = V.Object(field);

        Assert.Equal(PresencePolicy.AllowMissing | PresencePolicy.AllowNull, schema.InputPresence);
        Assert.Equal(PresencePolicy.Required, schema.OutputPresence);
        Assert.Equal(IssueCode.Required, V.ValidateJson(root, "{}").Issues[0].Code);
        Assert.Equal(IssueCode.NullNotAllowed,
            V.ValidateJson(root, "{\"value\":null}").Issues[0].Code);
        Assert.Equal(3, V.ValidateJson(root, "{\"value\":\"abc\"}").Output.Get(field));
        Assert.False(V.ValidateJson(V.String().RequireValue().Nullable(), "null").IsValid);
    }

    [Fact]
    public void OutputPresenceTracksEachStateChangingStage()
    {
        var nullable = V.String().Nullable().TransformNullable<string>(value => null);
        var present = nullable.RequireValue();
        var nullableAgain = present.TransformNullable<string>(value => null);

        Assert.Equal(PresencePolicy.AllowNull, nullable.OutputPresence);
        Assert.Equal(PresencePolicy.Required, present.OutputPresence);
        Assert.Equal(PresencePolicy.AllowNull, nullableAgain.OutputPresence);
        Assert.False(V.ValidateJson(present, "\"abc\"").IsValid);
    }

    [Fact]
    public void FailedChecksSuppressDependentTransforms()
    {
        int calls = 0;
        var schema = V.Integer().Min(1).Transform(value =>
        {
            calls++;
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }).NotBlank();

        Assert.False(V.ValidateJson(schema, "0").IsValid);
        Assert.Equal(0, calls);
        Assert.Equal("2", V.ValidateJson(schema, "2").Output);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void CallbackRegexExceptionsAreNotMistakenForTheBuiltinDeadline()
    {
        var schema = V.String().Check(value => throw new RegexMatchTimeoutException(),
            "callback", "Invalid value.");

        Assert.Throws<RegexMatchTimeoutException>(() => V.ValidateJson(schema, "\"abc\""));
    }

    [Fact]
    public void ACheckedRelationshipCollectionKeepsItsShape()
    {
        var line = V.Item("Part BOM").Property("quantity", V.DecimalText().Min(1m));
        var rows = V.Array(line).Optional().MinCount(1)
            .Check(values => values.Count <= 100, "limit", "Too many rows.");
        var bom = V.Relationships("Part BOM", rows);
        var count = V.Relationships("Part BOM", rows.Transform(values => values.Count));
        const string aml = "<Item type='Part'><Relationships><Item type='Part BOM'>" +
            "<quantity>2</quantity></Item></Relationships></Item>";

        Assert.Single(V.ValidateAml(V.Item("Part", bom), aml).Output.Get(bom));
        Assert.Equal(1, V.ValidateAml(V.Item("Part", count), aml).Output.Get(count));
        Assert.Throws<ArgumentException>(() => V.Relationships("Part BOM", line));
    }

    [Fact]
    public void SchemaMessageOverridesDoNotLeakIntoChildrenOrTheOriginal()
    {
        var original = V.String();
        var child = V.Field("name", original.WithMessage(IssueCode.Required, "Enter a name."));
        var root = V.Object(child).WithMessage(IssueCode.Required, "Enter an object.");

        Assert.Equal("Enter a name.", V.ValidateJson(root, "{}").Issues[0].Message);
        Assert.Equal("A value is required.", V.ValidateJson(V.Object()
            .Property("name", original), "{}").Issues[0].Message);
        var plain = V.Object().Property("name", original)
            .WithMessage(IssueCode.Required, "Enter an object.");
        Assert.Equal("A value is required.", V.ValidateJson(plain, "{}").Issues[0].Message);
    }

    [Fact]
    public void ReusableSchemasSupportCheckedAndTransformedRoots()
    {
        var reusable = new LengthSchema();

        Assert.Equal(3, V.ValidateJson(reusable, "\" abc \"").Output);
        Assert.Equal(3, V.ValidateJson(LengthSchema.Definition, "\" abc \"").Output);
        Assert.False(V.ValidateJson(reusable, "\" \"").IsValid);
    }

    private sealed class LengthSchema : Schema<int>
    {
        public static Schema<int> Definition { get; } = V.String()
            .Transform(value => value.Trim()).NotBlank().Transform(value => value.Length);

        public LengthSchema() : base(Definition)
        {
        }
    }
}
