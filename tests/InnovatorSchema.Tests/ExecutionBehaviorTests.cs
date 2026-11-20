using System;
using System.Text.Json;
using System.Threading;
using Xunit;
using V = InnovatorSchema.Validator;

namespace InnovatorSchema.Tests;

public sealed class ExecutionBehaviorTests
{
    [Fact]
    public void BooleanRepresentationIsExplicit()
    {
        Assert.True(V.ValidateJson(V.Boolean(), "true").Output);
        Assert.False(V.ValidateJson(V.Boolean(), "\"1\"").IsValid);
        Assert.True(V.ValidateJson(V.BooleanText(), "\"1\"").Output);
        Assert.False(V.ValidateJson(V.BooleanText(), "\"true\"").IsValid);
        Assert.True(V.ValidateXml(V.BooleanText(BooleanTextFormat.Either), "<flag>true</flag>")
            .Output);
        Assert.False(V.ValidateXml(V.Boolean(), "<flag>1</flag>").IsValid);
        Assert.False(V.ValidateXml(V.BooleanText(), "<flag> 1 </flag>").IsValid);
    }

    [Fact]
    public void UnexpectedTransformNullFailsValidation()
    {
        // A C# 6 callback can return null despite modern nullable annotations.
        var field = V.Field("name", V.String().Transform<string>(value => null!));
        var result = V.ValidateJson(V.Object(field), "{\"name\":\"P-100\"}");

        Assert.False(result.IsValid);
        Assert.False(result.HasOutput);
        Assert.Equal(IssueCode.NullNotAllowed, Assert.Single(result.Issues).Code);
        Assert.Equal("$.name", result.Issues[0].Path.ToString());
    }

    [Fact]
    public void PathRolesCannotBeConfusedWithPropertyNames()
    {
        var property = V.ValidateJson(V.Object().Property("@action", V.String()),
            "{\"@action\":0}");
        var attribute = V.ValidateAml(V.Item("Part", V.Attribute("action", V.String())),
            "<Item type='Part'/>");
        var punctuation = V.ValidateJson(V.Object().Property("a.b", V.String()), "{}");

        Assert.Equal("$[\"@action\"]", property.Issues[0].Path.ToString());
        Assert.Equal("$[@\"action\"]", attribute.Issues[0].Path.ToString());
        Assert.Equal("$[\"a.b\"]", punctuation.Issues[0].Path.ToString());
        Assert.Equal(PathSegmentKind.Property, property.Issues[0].Path[0].Kind);
        Assert.Equal(PathSegmentKind.Attribute, attribute.Issues[0].Path[0].Kind);
    }

    [Fact]
    public void CancellationIsObservedAtEntryAndCompletion()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var options = new ValidationOptions(cancellationToken: cancellation.Token);

        Assert.Throws<OperationCanceledException>(() => V.ValidateJson(V.String(), null, options));
        Assert.Throws<OperationCanceledException>(() => V.ValidateJson(V.String(), "{", options));

        using var during = new CancellationTokenSource();
        var schema = V.String().Transform(value =>
        {
            during.Cancel();
            return value;
        });

        Assert.Throws<OperationCanceledException>(() => V.ValidateJson(schema, "\"input\"",
            new ValidationOptions(cancellationToken: during.Token)));
    }

    [Fact]
    public void RegexTimeoutReturnsALimitIssue()
    {
        var schema = V.String().Matches("^(a+)+$");
        string input = JsonSerializer.Serialize(new string('a', 10_000) + "!");
        var result = V.ValidateJson(schema, input);

        Assert.False(result.IsValid);
        Assert.Equal(IssueCode.LimitExceeded, result.Issues[0].Code);
    }
}
