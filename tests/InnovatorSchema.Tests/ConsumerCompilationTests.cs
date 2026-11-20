using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace InnovatorSchema.Tests;

public sealed class ConsumerCompilationTests
{
    [Fact]
    public void DocumentedDeclarationsCompileWithCSharp6()
    {
        var target = (TargetFrameworkAttribute?)Attribute.GetCustomAttribute(
            typeof(Schema<string>).Assembly, typeof(TargetFrameworkAttribute));
        Assert.NotNull(target);
        Assert.Equal(new FrameworkName(target.FrameworkName).Version.Major,
            Environment.Version.Major);
        string path = Path.Combine(RepositoryFiles.Root, "examples", "SchemaDeclarations.cs");
        var compilation = Compile(File.ReadAllText(path));
        using var output = new MemoryStream();
        var result = compilation.Emit(output);

        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
    }

    [Fact]
    public void IncompatibleBuiltInRulesFailCompilation()
    {
        const string source =
            "using InnovatorSchema; using V = InnovatorSchema.Validator;" +
            "class Invalid { void Run() { var schema = V.Decimal().MaxLength(4); } }";

        var compilation = Compile(source);

        Assert.Contains(compilation.GetDiagnostics(),
            diagnostic => diagnostic.Id is "CS1061" or "CS1929");
    }

    [Fact]
    public void IncompatibleCustomPredicatesFailCompilation()
    {
        const string source =
            "using V = InnovatorSchema.Validator;" +
            "class Invalid { void Run() { var schema = V.String().Check(" +
            "(decimal value) => value > 0m, \"id\", \"Text\"); } }";

        var errors = Compile(source).GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        Assert.Contains(errors, diagnostic => diagnostic.Id is "CS1661" or "CS1678");
    }

    [Fact]
    public void IncompatiblePipelineStagesFailCompilation()
    {
        const string source =
            "using V = InnovatorSchema.Validator;" +
            "class Invalid { void Run() { var schema = V.String()" +
            ".Transform(value => value.Length).Check(V.NotBlank()); } }";

        Assert.Contains(Compile(source).GetDiagnostics(), diagnostic => diagnostic.Id == "CS1503");
    }

    [Fact]
    public void TypedRulesNullableTransformsAndReusableRootsCompileWithCSharp6()
    {
        const string source = @"using InnovatorSchema;
using V = InnovatorSchema.Validator;
class Reusable : Schema<int>
{
    public Reusable() : base(V.String().Transform(value => value.Trim())
        .NotBlank().Transform(value => value.Length)) { }
}
class Consumer
{
    void Run()
    {
        var bounds = V.Min(1m).And(V.Max(10m));
        var number = V.DecimalText().Check(bounds);
        var array = V.Array(V.String()).Optional().MinCount(1);
        var text = V.String().TransformNullable(value => value.Length == 0 ? null : value);
        var amount = V.Integer().TransformNullable(value => value > 0 ? (decimal?)value : null);
        var result = V.ValidateJson(new Reusable(), ""\""abc\"""");
        Field<int> count = V.Relationships(""Part BOM"",
            V.Array(V.Item(""Part BOM"")).Transform(values => values.Count));
    }
}";

        using var output = new MemoryStream();
        var result = Compile(source).Emit(output);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
    }

    internal static CSharpCompilation Compile(string source)
    {
        var options = new CSharpParseOptions(LanguageVersion.CSharp6);
        var tree = CSharpSyntaxTree.ParseText(source, options);

        return CSharpCompilation.Create(
            "ConsumerExamples",
            new[] { tree },
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static IEnumerable<MetadataReference> References()
    {
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is not string assemblies)
        {
            throw new InvalidOperationException("Runtime assembly references are unavailable.");
        }

        string[] runtimePaths = assemblies.Split(Path.PathSeparator);
        var paths = new HashSet<string>(runtimePaths, StringComparer.Ordinal)
        {
            typeof(Validator).Assembly.Location,
            typeof(Schema<string>).Assembly.Location
        };
#if ARAS_TESTS
        paths.Add(typeof(Aras.IOM.Item).Assembly.Location);
#endif

        if (paths.Count > 1024)
        {
            throw new InvalidOperationException("Consumer compilation has too many references.");
        }

        foreach (string path in paths)
        {
            yield return MetadataReference.CreateFromFile(path);
        }
    }
}
