using Xunit;

namespace InnovatorSchema.Tests;

public sealed class PresenceTests
{
    [Fact]
    public void MissingAndNullAreIndependentPermissions()
    {
        var original = Validator.String().NotBlank();
        var optional = original.Optional();
        var nullable = original.Nullable();

        Assert.Equal(PresencePolicy.Required, original.InputPresence);
        Assert.Equal(PresencePolicy.AllowMissing, optional.InputPresence);
        Assert.Equal(PresencePolicy.AllowNull, nullable.InputPresence);
        Assert.Equal(PresencePolicy.AllowMissing | PresencePolicy.AllowNull,
            optional.Nullable().InputPresence);
        Assert.Equal(nullable.Optional().InputPresence, optional.Nullable().InputPresence);
        Assert.Equal(optional.InputPresence, optional.Optional().InputPresence);
        Assert.False(Validator.ValidateJson(original, "null").IsValid);
        Assert.True(Validator.ValidateJson(nullable, "null").OutputValue.IsNull);
    }

    [Fact]
    public void NumericPresenceChangesPreserveTextParsingAndBounds()
    {
        var original = Validator.DecimalText().Min(0m);
        var derived = original.Nullable().Optional().Max(100m);
        var field = Validator.Field("weight", derived);
        var schema = Validator.Object(field);

        Assert.True(Validator.ValidateJson(schema, "{}").Output.Read(field).IsMissing);
        Assert.True(Validator.ValidateJson(schema, "{\"weight\":null}")
            .Output.Read(field).IsNull);
        Assert.Equal(2.5m, Validator.ValidateJson(schema, "{\"weight\":\"2.5\"}")
            .Output.Get(field));
        Assert.False(Validator.ValidateJson(schema, "{\"weight\":2.5}").IsValid);
        Assert.False(Validator.ValidateJson(schema, "{\"weight\":\"101\"}").IsValid);
        Assert.True(Validator.ValidateJson(original, "\"101\"").IsValid);
    }

    [Fact]
    public void BooleanPresenceChangesPreserveTheSelectedGrammar()
    {
        var original = Validator.BooleanText(BooleanTextFormat.Either);
        var derived = original.Optional().Nullable()
            .Check(value => value, "required.true", "Use true.");

        Assert.True(Validator.ValidateJson(derived, "\"true\"").Output);
        Assert.True(Validator.ValidateJson(derived, "null").OutputValue.IsNull);
        Assert.False(Validator.ValidateJson(derived, "\"false\"").IsValid);
        Assert.True(Validator.ValidateJson(original, "\"false\"").IsValid);
        Assert.Equal(PresencePolicy.Required, original.InputPresence);
    }
}
