using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace InnovatorSchema.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void PublicApiLivesInOneAssembly()
    {
        var assembly = typeof(Validator).Assembly;

        Assert.Equal("InnovatorSchema", assembly.GetName().Name);
        Assert.Same(assembly, typeof(Schema<string>).Assembly);
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(),
            name => name.Name?.StartsWith("InnovatorSchema.", StringComparison.Ordinal) == true);
        Assert.Single(Directory.EnumerateFiles(Path.Combine(RepositoryFiles.Root, "src"),
            "*.csproj", SearchOption.AllDirectories));
    }

    [Fact]
    public void PublicTypesUseOneNamespace()
    {
        var assembly = typeof(Validator).Assembly;

        Assert.All(assembly.GetExportedTypes(), type =>
            Assert.Equal("InnovatorSchema", type.Namespace));
        Assert.NotNull(assembly.GetType("InnovatorSchema.Validator"));
    }

    [Fact]
    public void RuntimeDependenciesAreFrameworkOnly()
    {
        var external = typeof(Validator).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null &&
                !name.StartsWith("System.", StringComparison.Ordinal) &&
                name is not ("mscorlib" or "netstandard"));

        Assert.Empty(external);
    }

    [Fact]
    public void CoreLayersCompileWithoutIom()
    {
        string root = Path.Combine(RepositoryFiles.Root, "src", "InnovatorSchema");
        var trees = new List<SyntaxTree>();
        var options = new CSharpParseOptions(LanguageVersion.CSharp10);

        foreach (string path in RepositoryFiles.CSharpSources())
        {
            if (path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                trees.Add(CSharpSyntaxTree.ParseText(File.ReadAllText(path), options, path));
            }
        }

        Assert.NotEmpty(trees);
        string runtime = Path.GetDirectoryName(typeof(object).Assembly.Location) ??
            throw new InvalidOperationException("The runtime directory is unavailable.");
        string assemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ??
            throw new InvalidOperationException("Runtime assembly references are unavailable.");
        var comparison = OperatingSystem.IsWindows() ?
            StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var references = assemblies.Split(Path.PathSeparator)
            .Where(path => string.Equals(Path.GetDirectoryName(path), runtime, comparison))
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("CoreBoundary", trees, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
        var errors = compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        Assert.Empty(errors);
    }
}
