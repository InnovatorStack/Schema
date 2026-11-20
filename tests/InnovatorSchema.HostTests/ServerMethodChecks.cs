using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Aras.IOM;
using InnovatorSchema.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace InnovatorSchema.HostTests;

internal static class ServerMethodChecks
{
    private static readonly string[] Names = { "ValidateJson", "ValidateAml", "ValidateXml" };
    private static readonly JsonSerializerOptions ExportOptions = new() { WriteIndented = true };

    public static void Export(string path)
    {
        using var output = File.Create(path);
        JsonSerializer.Serialize(output, new
        {
            LibraryMvid = typeof(Validator).Assembly.ManifestModule.ModuleVersionId,
            LibrarySha256 = Convert.ToHexString(SHA256.HashData(
                File.ReadAllBytes(typeof(Validator).Assembly.Location))),
            Methods = Names.Select(name => new
            {
                Name = name,
                Code = File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
                    "ServerMethods", name + ".cs"))
            }).ToArray(),
            Cases = ServerMethodCases.All().Select(test => new
            {
                test.Name,
                test.Method,
                RequestBody = test.Body(),
                test.Issues,
                test.Output,
                test.Error
            }).ToArray()
        }, ExportOptions);
    }

    public static void Check()
    {
        var methods = Names.ToDictionary(name => name, Compile, StringComparer.Ordinal);
        Run((name, body) =>
        {
            Type method = methods[name];
            var input = Activator.CreateInstance(method,
                OfflineConnection.CreateInnovator().getConnection()) as Item ??
                throw new InvalidOperationException("Could not create the Method input.");
            input.loadAML("<Item>" + body + "</Item>");
            return method.GetMethod("Run")?.Invoke(input, null) as Item ??
                throw new InvalidOperationException("The Method returned no Item.");
        }, "offline Method wrapper");
    }

    public static void Run(Func<string, string, Item> invoke, string transport)
    {
        Guid module = typeof(Validator).Assembly.ManifestModule.ModuleVersionId;
        var failures = new List<string>();
        int count = 0;

        foreach (ServerMethodCase test in ServerMethodCases.All())
        {
            count++;
            var timer = Stopwatch.StartNew();

            try
            {
                Item response = invoke(test.Method, test.Body());
                Verify(test, response, module);
                Console.WriteLine("PASS " + test.Name + " (" +
                    timer.Elapsed.TotalMilliseconds.ToString("F2",
                        System.Globalization.CultureInfo.InvariantCulture) + " ms)");
            }
            catch (Exception exception)
            {
                string error = exception.GetBaseException().Message;
                failures.Add(test.Name + ": " + error);
                Console.Error.WriteLine("FAIL " + test.Name + ": " + error);
            }
        }

        Console.WriteLine(transport + ": " + (count - failures.Count) + "/" + count + " passed.");

        if (failures.Count != 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
        }
    }

    private static void Verify(ServerMethodCase test, Item response, Guid module)
    {
        if (test.Error is not null)
        {
            if (!response.isError() ||
                !response.getErrorString().Contains(test.Error, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected Aras error was not returned.");
            }

            return;
        }

        if (response.isError())
        {
            throw new InvalidOperationException(
                "Unexpected Aras error: " + response.getErrorString());
        }

        using var result = JsonDocument.Parse(response.getResult());
        test.Verify(result.RootElement, module);
    }

    private static Type Compile(string name)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "ServerMethods", name + ".cs");
        string body = File.ReadAllText(path).TrimEnd('\r', '\n');

        if (body.Split('\n').Length > 68)
        {
            throw new InvalidOperationException(name + " exceeds the 70-line Method limit.");
        }

        string source = "using Aras.IOM; using System.Linq; public class TestMethod : Item { " +
            "public TestMethod(IServerConnection connection) : base(connection) {} " +
            "public Item Run() {\n" + body + "\n}}";
        var tree = CSharpSyntaxTree.ParseText(source,
            new CSharpParseOptions(LanguageVersion.CSharp6));
        string runtime = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ??
            throw new InvalidOperationException("Runtime references are unavailable.");
        var paths = runtime.Split(Path.PathSeparator).Concat(new[]
        {
            typeof(Validator).Assembly.Location, typeof(Item).Assembly.Location,
            Assembly.Load("Newtonsoft.Json").Location
        }).Distinct(StringComparer.Ordinal);
        var compilation = CSharpCompilation.Create("ServerCheck" + name, new[] { tree },
            paths.Select(file => MetadataReference.CreateFromFile(file)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var output = new MemoryStream();
        var result = compilation.Emit(output);

        if (!result.Success)
        {
            throw new InvalidOperationException(
                string.Join(Environment.NewLine, result.Diagnostics));
        }

        return Assembly.Load(output.ToArray()).GetType("TestMethod", throwOnError: true)!;
    }
}
