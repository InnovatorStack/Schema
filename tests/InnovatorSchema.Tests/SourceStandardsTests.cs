using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace InnovatorSchema.Tests;

public sealed class SourceStandardsTests
{
    private static readonly string[] Symbols = { "ARAS_TESTS" };

    [Fact]
    public void OwnedSourceRespectsPhysicalLimits()
    {
        var failures = new List<string>();

        foreach (string path in RepositoryFiles.CSharpSources())
        {
            Assert.True(new FileInfo(path).Length <= 4 * 1024 * 1024, "C# source exceeds 4 MiB.");
            string source = File.ReadAllText(path);
            string relativePath = Path.GetRelativePath(RepositoryFiles.Root, path);

            CheckSource(source, relativePath, failures);
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Theory]
    [InlineData(70, 0)]
    [InlineData(71, 1)]
    public void FunctionLimitCountsPhysicalLines(int length, int expected)
    {
        var body = new System.Text.StringBuilder("class Example\n{\nvoid Run()\n{\n");

        for (int index = 0; index < length - 3; index++)
        {
            body.AppendLine("// Comment.");
        }

        body.AppendLine("}\n}");
        var failures = new List<string>();

        CheckSource(body.ToString(), "Example.cs", failures);

        Assert.Equal(expected, failures.Count);
    }

    private static void CheckSource(string source, string path, List<string> failures)
    {
        var options = new CSharpParseOptions(LanguageVersion.CSharp10,
            preprocessorSymbols: Symbols);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, options);

        foreach (var line in tree.GetText().Lines)
        {
            if (line.Span.Length > 100)
            {
                failures.Add($"{path}:{line.LineNumber + 1}: line exceeds 100 columns.");
            }
        }

        foreach (SyntaxNode node in tree.GetRoot().DescendantNodes())
        {
            if (node is not (BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or
                AccessorDeclarationSyntax or AnonymousFunctionExpressionSyntax))
            {
                continue;
            }

            FileLinePositionSpan span = node.GetLocation().GetLineSpan();
            int length = span.EndLinePosition.Line - span.StartLinePosition.Line + 1;

            if (length > 70)
            {
                int firstLine = span.StartLinePosition.Line + 1;
                failures.Add($"{path}:{firstLine}: function exceeds 70 lines.");
            }
        }
    }
}
