using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.CSharp.RuntimeBinder;
using Xunit;
using V = InnovatorSchema.Validator;

namespace InnovatorSchema.Tests;

public sealed class ParseTests
{
    [Fact]
    public void AmlDomAndSelectedNodesWorkWithoutAnIomAssemblyReference()
    {
        var row = V.Item("Part").Property("item_number", V.String().NotBlank());
        var document = new XmlDocument();
        document.LoadXml("<AML><Item type='Part'><item_number>P-100</item_number></Item>" +
            "<Item type='Part'><item_number>P-200</item_number></Item></AML>");
        var nodes = document.SelectNodes("/AML/Item") ??
            throw new InvalidOperationException("Fixture nodes are missing.");
        var dom = Present((object?)V.Parse(V.Array(row), document));
        var selection = Present((object?)V.Parse(V.Array(row), nodes));
        string first = dom[0].item_number;
        string second = selection[1].item_number;
        Assert.Equal("P-100", first);
        Assert.Equal("P-200", second);
        Assert.False(V.SafeParse(row, document).Success);
    }

    [Fact]
    public void UnsupportedObjectsAreApiErrorsRatherThanValidationFailures()
    {
        Assert.Throws<ArgumentException>(() => V.SafeParse(V.Item("Part"), new object()));
        Assert.Throws<ArgumentException>(() => V.Parse(V.Item("Part"), new object()));
        Assert.Throws<ArgumentNullException>(() =>
            V.SafeParse(V.Item("Part"), (object)null!));
    }

    [Theory]
    [InlineData("<AML/>", 0)]
    [InlineData("<AML><Item type='Part'><item_number>P-100</item_number></Item></AML>", 1)]
    public void AmlCollectionsWorkWithoutAnIomContext(string input, int expected)
    {
        var schema = V.Array(V.Item("Part").Property("item_number", V.String().NotBlank()));
        var output = Present((object?)V.Parse(schema, input, InputFormat.Aml));
        int count = output.Count;
        Assert.Equal(expected, count);
    }

    [Fact]
    public void ArrayExtractionReusesAReadOnlyView()
    {
        var output = V.ValidateJson(V.Array(V.Integer()), "[1,2]").Output;
        Assert.Same(output.GetValues(), output.GetValues());
        Assert.Equal(new long[] { 1, 2 }, output.GetValues());
    }

    [Fact]
    public void InlineSchemaCreatesNamedRuntimeMembersAndIgnoresExtraFields()
    {
        var schema = V.Object()
            .Property("item_number", V.String().Transform(value => value.Trim()).NotBlank())
            .Property("weight", V.DecimalText().Min(0m));
        var output = Present((object?)V.Parse(schema,
            "{\"item_number\":\" P-100 \",\"weight\":\"2.5\",\"unrelated\":123}"));
        Assert.NotNull(output);
        string number = output.item_number;
        decimal weight = output.weight;
        Assert.Equal("P-100", number);
        Assert.Equal(2.5m, weight);
        var members = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(output);
        Assert.False(members.ContainsKey("unrelated"));
        Assert.Throws<RuntimeBinderException>(() => ReadUnknown(output));
    }

    [Fact]
    public void NestedObjectsArraysAndNullPositionsMaterializeRecursively()
    {
        var row = V.Object().Property("quantity", V.Decimal().Min(0m));
        var schema = V.Object().Property("lines", V.Array(row.Nullable()));
        var output = Present((object?)V.Parse(schema, "{\"lines\":[{\"quantity\":2.5},null]}"));
        Assert.NotNull(output);
        decimal quantity = output.lines[0].quantity;
        int count = output.lines.Count;
        object? second = output.lines[1];
        Assert.Equal(2.5m, quantity);
        Assert.Equal(2, count);
        Assert.Null(second);
        string json = JsonSerializer.Serialize((object)output);
        using var document = JsonDocument.Parse(json);
        Assert.Equal(2.5m, document.RootElement.GetProperty("lines")[0]
            .GetProperty("quantity").GetDecimal());
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("lines")[1].ValueKind);
    }

    [Fact]
    public void OptionalOmissionAndExplicitNullRemainDistinguishable()
    {
        var schema = V.Object().Property("note", V.String().Optional().Nullable());
        var omitted = Present((object?)V.Parse(schema, "{}"));
        var explicitNull = Present((object?)V.Parse(schema, "{\"note\":null}"));
        Assert.NotNull(omitted);
        Assert.NotNull(explicitNull);
        object? first = omitted.note;
        object? second = explicitNull.note;
        Assert.Null(first);
        Assert.Null(second);
        Assert.False((bool)omitted.ContainsKey("note"));
        Assert.True((bool)explicitNull.ContainsKey("note"));
        Assert.Equal("{}", JsonSerializer.Serialize((object)omitted));
        Assert.Equal("{\"note\":null}", JsonSerializer.Serialize((object)explicitNull));
    }

    [Fact]
    public void RootPresenceIsPreservedWithoutInventingDefaultValues()
    {
        var nullable = V.SafeParse(V.Integer().Nullable(), "null");
        Assert.True(nullable.Success);
        Assert.Equal(ValueState.Null, nullable.DataState);
        Assert.Null((object?)nullable.Data);
        Assert.Null((object?)V.Parse(V.Integer().Nullable(), "null"));
        var transformed = V.SafeParse(V.String().TransformNullable<string>(value => null),
            "\"value\"");
        Assert.True(transformed.Success);
        Assert.Equal(ValueState.Null, transformed.DataState);
        var missing = V.SafeParse(V.Integer().Optional(), (string?)null);
        Assert.True(missing.Success);
        Assert.Equal(ValueState.Missing, missing.DataState);
        Assert.Null((object?)missing.Data);
    }

    [Fact]
    public void AllIssuesAndTheSameStructuredErrorAreAvailableWithoutThrowing()
    {
        var schema = V.Object()
            .Property("name", V.String().MaxLength(3, "Use at most three characters."))
            .Property("age", V.Integer().Min(1L, "Enter a positive age."));
        const string input = "{\"name\":\"long name\",\"age\":0}";
        var safe = V.SafeParse(schema, input);
        Assert.False(safe.Success);
        Assert.NotNull(safe.Error);
        Assert.Equal(2, safe.GetIssues().Count);
        Assert.Same(safe.Issues, safe.Error.GetIssues());
        Assert.Equal("$.name", safe.Issues[0].Path.ToString());
        Assert.Equal("$.age", safe.Issues[1].Path.ToString());
        Assert.Contains("Problems:\n- $.name:", safe.Error.GetErrorString(),
            StringComparison.Ordinal);
        Assert.Contains("\n- $.age:", safe.GetErrorString(), StringComparison.Ordinal);
        var exception = Assert.Throws<ValidationException>(() => V.Parse(schema, input));
        Assert.Equal(safe.GetErrorString(), exception.Message);
        Assert.Same(exception.Error.Issues, exception.Issues);
        Assert.Equal(ValueState.Invalid, safe.DataState);
        Assert.Null((object?)safe.Data);
    }

    [Fact]
    public void FirstIssueRemainsAnExplicitOption()
    {
        var schema = V.Object().Property("a", V.String()).Property("b", V.String());
        var result = V.SafeParse(schema, "{}", options:
            new ValidationOptions(mode: ValidationMode.FirstIssue));
        Assert.Single(result.GetIssues());
        Assert.Equal("$.a", result.GetIssues()[0].Path.ToString());
    }

    [Fact]
    public void CallerExceptionsAndCancellationKeepTheirTypesAndIdentity()
    {
        var failure = new JsonException("Caller failure.");
        var schema = V.String().Transform<string>(value => throw failure);
        Assert.Same(failure, Assert.Throws<JsonException>(() => V.SafeParse(schema, "\"x\"")));
        Assert.Same(failure, Assert.Throws<JsonException>(() => V.Parse(schema, "\"x\"")));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var options = new ValidationOptions(cancellationToken: cancellation.Token);
        Assert.Throws<OperationCanceledException>(() =>
            V.SafeParse(V.String(), "\"x\"", options: options));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            V.SafeParse(V.String(), "\"x\"", (InputFormat)99));
        Assert.Throws<ArgumentNullException>(() =>
            V.Parse((Schema<string>)null!, "\"x\""));
    }

    [Theory]
    [InlineData("{")]
    [InlineData("\"\\uD800\"")]
    [InlineData("12")]
    public void InputFailuresStayInsideSafeParseAndBecomeValidationExceptionsInParse(string input)
    {
        Assert.False(V.SafeParse(V.String(), input).Success);
        Assert.Throws<ValidationException>(() => V.Parse(V.String(), input));
    }

    [Fact]
    public void SchemaNamesNeedNotBeCSharpIdentifiers()
    {
        var schema = V.Object().Property("part-number", V.String())
            .Property("Count", V.Integer());
        var output = Present((object?)V.Parse(schema, "{\"part-number\":\"P-100\",\"Count\":7}"));
        Assert.NotNull(output);
        string number = output["part-number"];
        long count = output.Count;
        Assert.Equal("P-100", number);
        Assert.Equal(7L, count);
    }

    [Fact]
    public void StrictUnknownPropertyCheckingRemainsAvailable()
    {
        var schema = V.Object().Property("name", V.String())
            .UnknownProperties(UnknownPropertyHandling.Reject);
        var result = V.SafeParse(schema, "{\"name\":\"Ada\",\"extra\":1}");
        Assert.Equal(IssueCode.UnknownProperty, Assert.Single(result.Issues).Code);
    }

    [Fact]
    public void ReusedSchemasDoNotShareParsedOutput()
    {
        var schema = V.Object().Property("value", V.Integer());
        Parallel.For(0, 100, index =>
        {
            var output = Present((object?)V.Parse(schema, "{\"value\":" + index + "}"));
            Assert.NotNull(output);
            long actual = output.value;
            Assert.Equal(index, actual);
        });
    }

    private static dynamic Present(object? output)
    {
        Assert.NotNull(output);
        return output;
    }

    private static object ReadUnknown(dynamic value)
    {
        return value.unknown;
    }
}
