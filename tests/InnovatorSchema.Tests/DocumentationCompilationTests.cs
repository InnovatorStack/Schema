using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace InnovatorSchema.Tests;

public sealed class DocumentationCompilationTests
{
    [Fact]
    public void ApiExamplesCompileWithCSharp7()
    {
        string path = Path.Combine(RepositoryFiles.Root, "docs", "api", "index.md");
        string markdown = File.ReadAllText(path).ReplaceLineEndings("\n");
        var examples = new Regex("```csharp\n(.*?)```", RegexOptions.Singleline,
            TimeSpan.FromSeconds(1)).Matches(markdown);
        Assert.NotEmpty(examples);

        foreach (Match example in examples)
        {
            const string imports = "using System; using InnovatorSchema; " +
                "using V = InnovatorSchema.Validator; ";
            string source = imports + "class Example { void Run() { " +
                example.Groups[1].Value + " } }";
            using var output = new MemoryStream();
            var result = ConsumerCompilationTests.Compile(source).Emit(output);

            Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        }
    }
}
