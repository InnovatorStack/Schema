using System;
using System.Linq;
using System.Text;
using Xunit;
using V = InnovatorSchema.Validator;

namespace InnovatorSchema.Tests;

public sealed class ExecutionLimitsTests
{
    [Fact]
    public void FirstIssueModeStopsAtOneFailure()
    {
        var schema = V.Object().Property("known", V.String().NotBlank())
            .UnknownProperties(UnknownPropertyHandling.Reject);
        var options = new ValidationOptions(mode: ValidationMode.FirstIssue);
        var result = V.ValidateJson(schema, "{\"z\":1,\"a\":2,\"known\":\"\"}", options);

        Assert.Single(result.Issues);
        Assert.Equal("$.a", result.Issues[0].Path.ToString());
    }

    [Fact]
    public void IssuesAndWorkAreBounded()
    {
        var schema = V.Array(V.String().NotBlank());
        var result = V.ValidateJson(schema, "[\"\",\"\",\"\"]",
            new ValidationOptions(maximumIssues: 2));

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Issues.Count);
        Assert.Equal(IssueCode.LimitExceeded, result.Issues[1].Code);
        Assert.Equal(IssueCode.LimitExceeded, V.ValidateJson(
            V.String().NotBlank(), "\"value\"", new ValidationOptions(maximumOperations: 1))
            .Issues[0].Code);
    }

    [Fact]
    public void UnknownInputStillParticipatesInBudgets()
    {
        var schema = V.Object().UnknownProperties(UnknownPropertyHandling.Allow);

        Assert.Equal(IssueCode.LimitExceeded, V.ValidateJson(schema,
            "{\"ignored\":[1,2,3]}", new ValidationOptions(maximumCollectionLength: 2))
            .Issues[0].Code);
        Assert.Equal(IssueCode.LimitExceeded, V.ValidateJson(schema,
            "{\"ignored\":{\"nested\":1}}", new ValidationOptions(maximumDepth: 2))
            .Issues[0].Code);
    }

    [Fact]
    public void PipelineLengthIsBoundedSeparatelyFromInputDepth()
    {
        Schema<string> schema = V.String();

        for (int index = 0; index < 64; index++)
        {
            schema = schema.Transform(value => value);
        }

        Assert.Throws<ArgumentException>(() => schema.Transform(value => value));
    }

    [Fact]
    public void ErrorSummaryIsBounded()
    {
        string message = new('x', 1024);
        var schema = V.Array(V.String().NotBlank(message));
        string input = "[" + string.Join(",", Enumerable.Repeat("\"\"", 100)) + "]";
        var result = V.ValidateJson(schema, input);

        Assert.False(result.IsValid);
        Assert.InRange(result.GetErrorString().Length, 1, 8192);
        Assert.EndsWith("[truncated]", result.GetErrorString(), StringComparison.Ordinal);
    }

    [Fact]
    public void SummaryTruncationDoesNotSplitUnicodePairs()
    {
        string input = "[" + string.Join(",", Enumerable.Repeat("\"\"", 20)) + "]";

        for (int padding = 0; padding < 8; padding++)
        {
            string message = new string('x', padding) +
                string.Concat(Enumerable.Repeat("\U0001F642", 500));
            var schema = V.Array(V.String().NotBlank(message));
            string summary = V.ValidateJson(schema, input).GetErrorString();
            byte[] encoded = new UTF8Encoding(false, true).GetBytes(summary);
            Assert.NotEmpty(encoded);
            Assert.InRange(summary.Length, 1, 8192);
        }
    }
}
