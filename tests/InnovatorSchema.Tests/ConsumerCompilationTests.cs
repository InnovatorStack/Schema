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
    [InlineData("PartMethod.cs", "<Item type='Part' id='783BA98DA5D04BF9AF4B1760826221F0'" +
        " action='edit'><weight>0</weight>" +
        "<related_id><Item type='Part' id='938599DAAAE34871BFA51167C242DB12'/>" +
        "</related_id></Item>")]
    [InlineData("PartBatchMethod.cs",
        "<AML><Item type='Part' id='783BA98DA5D04BF9AF4B1760826221F0'>" +
        "<weight>1.5</weight></Item></AML>")]
    [InlineData("ParsePartMethod.cs",
        "<Item type='Part'><item_number>P-100</item_number><weight>4</weight></Item>",
        "P-100 weighs 4")]
    [InlineData("ParseBomMethod.cs",
        "<AML><Item type='Part BOM'><quantity>2.5</quantity>" +
        "<reference_designator>R1</reference_designator><related_id>" +
        "<Item type='Part' id='783BA98DA5D04BF9AF4B1760826221F0'>" +
        "<item_number>P-100</item_number><weight>1.5</weight></Item></related_id></Item>" +
        "<Item type='Part BOM'><quantity>2</quantity><related_id>" +
        "<Item type='Part' id='938599DAAAE34871BFA51167C242DB12'>" +
        "<item_number>P-200</item_number><weight>3</weight></Item></related_id></Item></AML>",
        "P-100 (R1): 3.75\nP-200: 6\nTotal weight: 9.75")]
    public void ExamplesCompileAndRunAgainstTheSelectedIom(string file, string aml,
        string? expectedResult = null)
    {
        string body = File.ReadAllText(Path.Combine(RepositoryFiles.Root, "examples", file));
        string source = "using InnovatorSchema; using V = InnovatorSchema.Validator;" +
            "using Aras.IOM; public class Example : Item { " +
            "public Example(IServerConnection connection) : base(connection) {} " +
            "public Item Run() { " + body + " } }";
        using var output = new MemoryStream();
        var languageVersion = expectedResult is null
            ? LanguageVersion.CSharp6 : LanguageVersion.CSharp7;
        var result = Compile(source, languageVersion).Emit(output);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));

        Type type = Assembly.Load(output.ToArray()).GetType("Example", throwOnError: true)!;
        var input = (Item)Activator.CreateInstance(type, OfflineConnection.CreateConnection())!;
        input.loadAML(aml);
        var returned = type.GetMethod("Run")!.Invoke(input, null);

        if (expectedResult is null)
        {
            Assert.Same(input, returned);
        }
        else
        {
            Assert.Equal(expectedResult, Assert.IsType<Item>(returned).getResult());
        }
    }

    internal static CSharpCompilation Compile(string source,
        LanguageVersion languageVersion = LanguageVersion.CSharp7)
    {
        var options = new CSharpParseOptions(languageVersion);
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
