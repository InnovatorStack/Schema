using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Xml.Linq;

namespace InnovatorSchema.HostTests;

internal sealed record ExpectedIssue(
    string Code, string Path, string? Message = null, string? RuleId = null);

internal sealed record ServerMethodCase(
    string Name, string Method, string Payload, ExpectedIssue[] Issues,
    string? Output = null, string? Error = null,
    IReadOnlyDictionary<string, string>? Options = null)
{
    public string Body()
    {
        var root = new XElement("Item", new XElement("payload", Payload));

        if (Options is not null)
        {
            foreach (var option in Options)
            {
                root.Add(new XElement(option.Key, option.Value));
            }
        }

        if (Error is not null)
        {
            root.Add(new XElement("return_error", "1"));
        }

        return string.Concat(root.Elements());
    }

    public void Verify(JsonElement result, Guid expectedModule)
    {
        Require(result.GetProperty("LibraryMvid").GetGuid() == expectedModule,
            "The server loaded a different InnovatorSchema.dll.");
        bool valid = Issues.Length == 0;
        Require(result.GetProperty("Valid").GetBoolean() == valid, "Validity differs.");
        Require(result.GetProperty("HasOutput").GetBoolean() == valid, "Output contract differs.");
        if (!valid)
        {
            Require(result.GetProperty(nameof(Output)).ValueKind == JsonValueKind.Null,
                "Invalid validation exposed an output.");
        }

        if (result.TryGetProperty("State", out var state))
        {
            Require(state.GetString() == (valid ? "Present" : "Invalid"),
                "Root output state differs.");
        }

        var actual = result.GetProperty(nameof(Issues));
        Require(actual.GetArrayLength() == Issues.Length, "Issue count differs.");

        for (int index = 0; index < Issues.Length; index++)
        {
            ExpectedIssue expected = Issues[index];
            var issue = actual[index];
            Require(issue.GetProperty("Code").GetString() == expected.Code, "Issue code differs.");
            Require(issue.GetProperty("Path").GetString() == expected.Path, "Issue path differs.");

            if (expected.Message is not null)
            {
                Require(issue.GetProperty("Message").GetString() == expected.Message,
                    "Custom message differs.");
            }

            Require(issue.GetProperty("RuleId").GetString() == expected.RuleId,
                "Rule identifier differs.");
        }

        if (Output is not null)
        {
            VerifyOutput(result.GetProperty(nameof(Output)));
        }

        if (result.TryGetProperty("InputUnchanged", out var unchanged))
        {
            Require(unchanged.GetBoolean(), "The supplied IOM Item was modified.");
        }
    }

    private void VerifyOutput(JsonElement actual)
    {
        using var expected = JsonDocument.Parse(Output!);

        foreach (JsonProperty property in expected.RootElement.EnumerateObject())
        {
            var value = actual.GetProperty(property.Name);
            Require(value.ValueKind == property.Value.ValueKind,
                "Output type differs for " + property.Name + ".");
            bool equal = value.ValueKind == JsonValueKind.Number ?
                value.GetDecimal() == property.Value.GetDecimal() :
                value.ToString() == property.Value.ToString();
            Require(equal, "Output differs for " + property.Name + ".");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
