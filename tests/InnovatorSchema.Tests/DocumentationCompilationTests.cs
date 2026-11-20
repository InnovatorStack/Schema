using System;
using System.IO;
using System.Linq;
using Xunit;

namespace InnovatorSchema.Tests;

public sealed class DocumentationCompilationTests
{
    [Fact]
    public void ApiExamplesCompileWithCSharp6()
    {
        string directory = Path.Combine(RepositoryFiles.Root, "docs", "api");
        string[] paths = Directory.GetFiles(directory, "*.md");
        Assert.NotEmpty(paths);

        foreach (string path in paths.OrderBy(value => value, StringComparer.Ordinal))
        {
            string name = Path.GetFileNameWithoutExtension(path);

            if (name == "index")
            {
                continue;
            }

            string example = Example(File.ReadAllText(path));
            string source = Wrap(name, example);
            using var output = new MemoryStream();
            var result = ConsumerCompilationTests.Compile(source).Emit(output);

            Assert.True(result.Success, name + "\n" + string.Join("\n", result.Diagnostics));
        }
    }

    private static string Example(string markdown)
    {
        markdown = markdown.ReplaceLineEndings("\n");
        const string heading = "## Example\n";
        int section = markdown.IndexOf(heading, StringComparison.Ordinal);
        Assert.True(section >= 0, "API page is missing its Example section.");
        const string fence = "```csharp\n";
        int start = markdown.IndexOf(fence, section, StringComparison.Ordinal);
        Assert.True(start >= 0, "API example is missing its C# fence.");
        start += fence.Length;
        int end = markdown.IndexOf("```", start, StringComparison.Ordinal);
        Assert.True(end >= start, "API example has an unclosed C# fence.");

        return markdown[start..end];
    }

    private static string Wrap(string name, string example)
    {
        const string imports = "using System; using InnovatorSchema; " +
            "using V = InnovatorSchema.Validator; ";

        return imports + "class Example { void Run() { " + Inputs(name) + example + " } }";
    }

    private static string Inputs(string name)
    {
        return name switch
        {
            "validate-json" or "validation-options" =>
                "var schema = V.String(); string json = \"\\\"P-100\\\"\"; ",
            "validate-xml" => "var schema = V.String(); string xml = \"<value>P-100</value>\"; ",
            "validate-aml" => "var schema = V.Item(\"Part\"); " +
                "string aml = \"<Item type='Part'/>\"; ",
            "issue-code" or "issue-path" or "validation-issue" =>
                "var result = V.ValidateJson(V.String(), null); ",
            "validation-result" or "validation-value" =>
                "var result = V.ValidateJson(V.String(), \"\\\"P-100\\\"\"); ",
            _ => string.Empty
        };
    }
}
