using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Aras.IOM;
using InnovatorSchema.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace InnovatorSchema.Tests;

public sealed class ConsumerCompilationTests
{
    [Theory]
    [InlineData("PartMethod.cs", "<Item type='Part' id='a' action='edit'><weight>0</weight>" +
        "<related_id><Item type='Part' id='b'/></related_id></Item>")]
    [InlineData("PartBatchMethod.cs",
        "<AML><Item type='Part' id='a'><weight>1.5</weight></Item></AML>")]
    public void ExamplesCompileAndRunAgainstTheSelectedIom(string file, string aml)
    {
        string body = File.ReadAllText(Path.Combine(RepositoryFiles.Root, "examples", file));
        string source = "using InnovatorSchema; using V = InnovatorSchema.Validator;" +
            "using Aras.IOM; public class Example : Item { " +
            "public Example(IServerConnection connection) : base(connection) {} " +
            "public Item Run() { " + body + " } }";
        using var output = new MemoryStream();
        var result = Compile(source).Emit(output);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));

        Type type = Assembly.Load(output.ToArray()).GetType("Example", throwOnError: true)!;
        var input = (Item)Activator.CreateInstance(type, OfflineConnection.CreateConnection())!;
        input.loadAML(aml);
        var returned = type.GetMethod("Run")!.Invoke(input, null);

        Assert.Same(input, returned);
    }

    internal static CSharpCompilation Compile(string source)
    {
        var options = new CSharpParseOptions(LanguageVersion.CSharp6);
        return CSharpCompilation.Create("ConsumerExamples",
            new[] { CSharpSyntaxTree.ParseText(source, options) }, References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static IEnumerable<MetadataReference> References()
    {
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is not string assemblies)
        {
            throw new InvalidOperationException("Runtime assembly references are unavailable.");
        }

        var paths = new HashSet<string>(assemblies.Split(Path.PathSeparator),
            StringComparer.Ordinal)
        {
            typeof(Validator).Assembly.Location,
            typeof(Item).Assembly.Location
        };

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
