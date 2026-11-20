using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using V = InnovatorSchema.Validator;

namespace InnovatorSchema.Tests;

public sealed class NestedValidationTests
{
    private static readonly string[] FailurePaths =
    {
        "$.lines[0].code", "$.lines[0].quantity", "$.lines[1].quantity"
    };

    [Fact]
    public void NestedPipelinesProduceTypedOwnedOutput()
    {
        var code = V.Field("code", V.String().NotBlank()
            .Transform(value => value.Trim()).Matches("^P-"));
        var quantity = V.Field("quantity", V.Decimal().Min(0.001m));
        var line = V.Object(code, quantity).Transform(row => new
        {
            Code = row.Get(code),
            Quantity = row.Get(quantity)
        });
        var lines = V.Field("lines", V.Array(line).MinCount(1));
        var schema = V.Object(lines).Transform(row => new { Lines = row.Get(lines).GetValues() });
        const string json = "{\"lines\":[{\"code\":\" P-100 \",\"quantity\":2.5}]}";

        var result = V.ValidateJson(schema, json);

        Assert.True(result.IsValid, result.GetErrorString());
        Assert.True(result.HasOutput);
        Assert.Equal("P-100", result.Output.Lines[0].Code);
        Assert.Equal(2.5m, result.Output.Lines[0].Quantity);
        Assert.Equal("{\"Lines\":[{\"Code\":\"P-100\",\"Quantity\":2.5}]}",
            JsonSerializer.Serialize(result.Output));
        Assert.Contains(" P-100 ", json, StringComparison.Ordinal);
    }

    [Fact]
    public void NestedFailuresHaveExactPathsAndSuppressObjectOutput()
    {
        var line = V.Object()
            .Property("code", V.String().NotBlank())
            .Property("quantity", V.Decimal().Min(1m));
        var schema = V.Object().Property("lines", V.Array(line));
        const string json = "{\"lines\":[{\"code\":\"\",\"quantity\":0}," +
            "{\"code\":\"P-200\",\"quantity\":\"2\"}]}";

        var result = V.ValidateJson(schema, json);

        Assert.False(result.IsValid);
        Assert.False(result.HasOutput);
        Assert.Equal(ValueState.Invalid, result.OutputValue.State);
        Assert.Throws<InvalidOperationException>(() => result.Output);
        Assert.Equal(FailurePaths, result.Issues.Select(issue => issue.Path.ToString()));
    }

    [Fact]
    public void FailedAndAbsentValuesDoNotInvokeTransforms()
    {
        int calls = 0;
        var value = V.Field("value", V.String().NotBlank().Optional().Nullable()
            .Transform(text =>
            {
                calls++;
                return text.Length;
            }));
        var schema = V.Object(value);

        Assert.True(V.ValidateJson(schema, "{}").Output.Read(value).IsMissing);
        Assert.True(V.ValidateJson(schema, "{\"value\":null}").Output.Read(value).IsNull);
        Assert.False(V.ValidateJson(schema, "{\"value\":\" \"}").IsValid);
        Assert.Equal(0, calls);
        Assert.Equal(3, V.ValidateJson(schema, "{\"value\":\"abc\"}").Output.Get(value));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void ArraysRetainNullableElementStates()
    {
        var result = V.ValidateJson(V.Array(V.Integer().Nullable()), "[1,null,3]");

        Assert.True(result.IsValid);
        Assert.Equal(3, result.Output.Count);
        Assert.Equal(1, result.Output[0].Value);
        Assert.True(result.Output[1].IsNull);
        Assert.Equal(3, result.Output[2].Value);
        Assert.Throws<InvalidOperationException>(() => result.Output.GetValues());
    }

    [Fact]
    public void TypedReadsRejectForeignHandles()
    {
        var code = V.Field("code", V.String());
        var other = V.Field("code", V.String());
        var result = V.ValidateJson(V.Object(code), "{\"code\":\"P-100\"}");

        Assert.Equal("P-100", result.Output.Get(code));
        Assert.Throws<ArgumentException>(() => result.Output.Get(other));
    }

    [Fact]
    public void CallbackParseExceptionsPropagate()
    {
        var schema = V.String().Transform<string>(
            value => throw new JsonException("Callback bug."));

        Assert.Throws<JsonException>(() => V.ValidateJson(schema, "\"value\""));
    }

    [Fact]
    public void OrdinaryInputFailuresDoNotThrow()
    {
        Assert.Equal(IssueCode.Required, V.ValidateJson(V.String(), null).Issues[0].Code);
        Assert.Equal(IssueCode.MalformedJson, V.ValidateJson(V.String(), "").Issues[0].Code);
        Assert.Equal(IssueCode.TypeMismatch, V.ValidateJson(V.String(), "2").Issues[0].Code);
        Assert.Equal(IssueCode.InvalidNumber,
            V.ValidateJson(V.Decimal(), "1.00000000000000000000000000001").Issues[0].Code);
        Assert.Equal(12.5m, V.ValidateJson(V.DecimalText(), "\"12.5\"").Output);
        Assert.Equal(1L, V.ValidateJson(V.Integer(), "1.0").Output);
    }

    [Fact]
    public void ConcurrentValidationIsIsolated()
    {
        var field = V.Field("value", V.Integer().Min(0));
        var schema = V.Array(V.Object(field));

        Parallel.For(0, 200, index =>
        {
            var result = V.ValidateJson(schema, "[{\"value\":" + index + "}]");
            Assert.True(result.IsValid, result.GetErrorString());
            Assert.Equal(index, result.Output[0].Value.Get(field));
        });

        Assert.Equal("$[0].value", V.ValidateJson(schema, "[{\"value\":-1}]")
            .Issues[0].Path.ToString());
    }
}
