using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Aras.IOM;
using InnovatorSchema.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace InnovatorSchema.HostTests;

internal static class MethodBodyChecks
{
    public static void Check()
    {
        MethodImports.Check();
        CheckPart();
        CheckJson();
        CheckAml();
        CheckTypedOutput();
        CheckParsedOutput();
        CheckAssembly();
    }

    private static void CheckAssembly()
    {
        string aml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Assembly.xml"));
        string source = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "AssemblyMethod.cs"));
        const string call = "V.Parse(schema, this)";
        Require(source.Contains(call, StringComparison.Ordinal), "The Parse example changed.");
        CheckAssemblyCases(CompileBody("AssemblyItem", source), aml, "IOM");
        string text = source.Replace(call, "V.Parse(schema, this.ToString(), InputFormat.Aml)",
            StringComparison.Ordinal);
        CheckAssemblyCases(CompileBody("AssemblyAml", text), aml, "AML");
        CheckSafeAssemblyCases(CompileBody("AssemblySafeItem", AssemblyProbe(source, false)), aml);
        CheckSafeAssemblyCases(CompileBody("AssemblySafeAml", AssemblyProbe(source, true)), aml);
    }

    private static void CheckAssemblyCases(Type method, string aml, string representation)
    {
        var input = Create(method);
        int count = 0;

        foreach (var test in AssemblyCases.Create(aml))
        {
            input.loadAML(test.Input);
            string before = input.ToString();

            try
            {
                decimal weight = decimal.Parse(Run(method, input).getResult(),
                    System.Globalization.CultureInfo.InvariantCulture);
                Require(test.Weight.HasValue && weight == test.Weight.Value,
                    test.Name + ": the parsed assembly weight differs.");
            }
            catch (TargetInvocationException exception)
                when (exception.InnerException is ValidationException failure)
            {
                var paths = failure.Issues.Select(issue => issue.Path.ToString());
                Require(!test.Weight.HasValue && paths.SequenceEqual(test.Paths),
                    test.Name + ": unexpected issues: " + string.Join(", ", paths));
            }

            Require(input.ToString() == before, test.Name + ": the source Item changed.");
            count++;
        }

        Console.WriteLine("PASS Complex assembly Parse " + representation +
            ": " + count + " cases.");
    }

    private static string AssemblyProbe(string source, bool text)
    {
        int start = source.IndexOf("var assembly = ", StringComparison.Ordinal);
        Require(start >= 0, "The assembly example has no Parse statement.");
        string input = text ? "this.ToString(), InputFormat.Aml" : "this";
        return source[..start] + "var result = V.SafeParse(schema, " + input + @");
return this.getInnovator().newResult(Newtonsoft.Json.JsonConvert.SerializeObject(new
{
    result.Success, result.Data,
    Paths = result.GetIssues().Select(issue => issue.Path.ToString()).ToArray()
}));";
    }

    private static void CheckSafeAssemblyCases(Type method, string aml)
    {
        var input = Create(method);
        int count = 0;

        foreach (var test in AssemblyCases.Create(aml))
        {
            input.loadAML(test.Input);
            string before = input.ToString();
            using var document = JsonDocument.Parse(Run(method, input).getResult());
            var root = document.RootElement;
            Require(root.GetProperty("Success").GetBoolean() == test.Weight.HasValue,
                test.Name + ": unexpected SafeParse success state.");
            var paths = root.GetProperty("Paths").EnumerateArray().Select(path => path.GetString());
            Require(paths.SequenceEqual(test.Paths), test.Name + ": unexpected SafeParse issues.");
            var data = root.GetProperty("Data");

            if (test.Weight.HasValue)
            {
                CheckAssemblyData(data);
            }
            else
            {
                Require(data.ValueKind == JsonValueKind.Null,
                    test.Name + ": invalid SafeParse returned data.");
            }

            Require(input.ToString() == before, test.Name + ": SafeParse modified the Item.");
            count++;
        }

        Console.WriteLine("PASS Complex assembly " + method.Assembly.GetName().Name +
            ": " + count + " cases.");
    }

    private static void CheckAssemblyData(JsonElement data)
    {
        Require(data.GetProperty("item_number").GetString() == "P-100",
            "The root transform was not materialized.");
        var first = data.GetProperty("Relationships").GetProperty("Part BOM")[0];
        Require(first.GetProperty("quantity").ValueKind == JsonValueKind.Number,
            "The quantity remained text.");
        Require(first.GetProperty("sort_order").GetInt64() == 10L,
            "The integer text was not converted.");
        var part = first.GetProperty("related_id");
        Require(part.GetProperty("item_number").GetString() == "P-200",
            "The nested transform was not materialized.");
        var drawing = part.GetProperty("Relationships").GetProperty("Part Document")[0]
            .GetProperty("related_id");
        Require(drawing.GetProperty("item_number").GetString() == "D-200",
            "The document transform was not materialized.");
        Require(!data.TryGetProperty("extra_root", out _) &&
            !first.TryGetProperty("extra_line", out _) &&
            !drawing.TryGetProperty("extra_document", out _), "Extra fields leaked into output.");
    }

    private static void CheckParsedOutput()
    {
        Type method = Compile("ParsePartMethod.cs");
        var input = Create(method);
        input.loadAML("<Item type='Part'><item_number>P-100</item_number>" +
            "<extra>allowed</extra></Item>");
        Require(Run(method, input).getResult() == "P-100", "Parsed field access differs.");
        input.setProperty("item_number", " ");
        ExpectValidationFailure(method, input, "$.item_number");
        method = Compile("ParsePartsMethod.cs");
        input = Create(method);
        input.loadAML("<Item type='MethodInput'><parts><Item type='Part'>" +
            "<item_number>P-200</item_number></Item><Item type='Part'>" +
            "<item_number>P-300</item_number></Item></parts></Item>");
        Require(Run(method, input).getResult() == "P-200", "Parsed collection access differs.");
        input.loadAML("<Item type='MethodInput'><parts><Item type='Part'/></parts></Item>");
        ExpectValidationFailure(method, input, "$[0].item_number");
    }

    private static void CheckAml()
    {
        Type method = Compile("AmlMethod.cs");
        var input = Create(method);
        input.loadAML("<Item type='Method'/>");
        input.setProperty("payload", "<Item type='Part'><item_number>P-100</item_number>" +
            "<Relationships><Item type='Part BOM'><quantity>2</quantity>" +
            "<related_id><Item type='Part'><item_number>P-200</item_number></Item></related_id>" +
            "</Item></Relationships></Item>");
        Require(ReferenceEquals(input, Run(method, input)), "Valid AML was not returned.");
        input.setProperty("payload", "<Item type='Part'><item_number>P-100</item_number></Item>");
        var error = Run(method, input);
        Require(error.isError(), "Missing BOM rows did not return an error.");
        Require(error.getErrorString().Contains("Part BOM", StringComparison.Ordinal),
            "AML Method lost the relationship issue path.");
    }

    private static void CheckPart()
    {
        Type method = Compile("PartMethod.cs");
        var part = Create(method);
        part.loadAML("<Item type='Part'><item_number> P-100 </item_number></Item>");
        string original = part.ToString();
        Require(ReferenceEquals(part, Run(method, part)), "Valid Part was not returned.");
        Require(part.ToString() == original, "Part Method modified the input.");
        part.setProperty("item_number", " ");
        Require(Run(method, part).isError(), "Invalid Part did not return an error.");
    }

    private static void CheckJson()
    {
        Type method = Compile("JsonMethod.cs");
        var first = Create(method);
        var second = Create(method);
        first.loadAML("<Item type='MethodInput'/>");
        second.loadAML("<Item type='MethodInput'/>");
        first.setProperty("payload", "{\"item_number\":\"P-100\",\"quantity\":2.5}");
        Require(ReferenceEquals(first, Run(method, first)), "Valid JSON was not returned.");
        first.setProperty("payload", "{}");
        second.setProperty("payload", "{}");
        var firstError = Run(method, first);
        var secondError = Run(method, second);
        Require(firstError.isError() && secondError.isError(), "Invalid JSON returned no error.");
        Require(ReferenceEquals(firstError.getInnovator().getConnection(),
            first.getInnovator().getConnection()), "The result lost the first context.");
        Require(ReferenceEquals(secondError.getInnovator().getConnection(),
            second.getInnovator().getConnection()), "The result reused another context.");
    }

    private static void CheckTypedOutput()
    {
        Type method = Compile("TypedOutputMethod.cs");
        var input = Create(method);
        input.loadAML("<Item type='MethodInput'/>");
        input.setProperty("payload", "{\"lines\":[{\"code\":\" P-100 \",\"quantity\":2.5}]}");
        Require(Run(method, input).getResult() == "2.5", "Typed output differs.");
        input.setProperty("payload", "{\"lines\":[{\"code\":\"X-100\",\"quantity\":2.5}]}");
        ExpectValidationFailure(method, input, "$.lines[0].code");
    }

    private static void ExpectValidationFailure(Type method, Item input, string path)
    {
        try
        {
            Run(method, input);
        }
        catch (TargetInvocationException exception)
            when (exception.InnerException is ValidationException failure)
        {
            Require(failure.GetIssues().Any(issue => issue.Path.ToString() == path),
                "The validation exception lost its issue path.");
            return;
        }

        throw new InvalidOperationException("Parse did not throw ValidationException.");
    }

    private static Type Compile(string file)
    {
        string method = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, file));
        return CompileBody(file, method);
    }

    private static Type CompileBody(string file, string method)
    {
        string source = @"using Aras.IOM;
using System.Linq;
public class PrototypeMethod : Item
{
    public PrototypeMethod(IServerConnection connection) : base(connection) { }
    public Item Run()
    {
" + method + @"
    }
}";
        var tree = MethodImports.Apply(source);
        var compilation = CSharpCompilation.Create(Path.GetFileNameWithoutExtension(file),
            new[] { tree }, References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var output = new MemoryStream();
        var result = compilation.Emit(output);
        Require(result.Success, string.Join(Environment.NewLine, result.Diagnostics));

        return Assembly.Load(output.ToArray()).GetType("PrototypeMethod", throwOnError: true) ??
            throw new InvalidOperationException("Compiled Method type is missing.");
    }

    private static IEnumerable<MetadataReference> References()
    {
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is not string runtime)
        {
            throw new InvalidOperationException("Runtime references are unavailable.");
        }

        var paths = new HashSet<string>(runtime.Split(Path.PathSeparator), StringComparer.Ordinal)
        {
            typeof(Validator).Assembly.Location,
            typeof(Schema<string>).Assembly.Location,
            typeof(Item).Assembly.Location
        };

        if (paths.Count > 1024)
        {
            throw new InvalidOperationException("Compilation reference limit exceeded.");
        }

        return paths.Select(path => MetadataReference.CreateFromFile(path));
    }

    private static Item Create(Type method)
    {
        return Activator.CreateInstance(method,
            OfflineConnection.CreateInnovator().getConnection()) as Item ??
            throw new InvalidOperationException("Could not create the compiled Method Item.");
    }

    private static Item Run(Type method, Item input)
    {
        return method.GetMethod("Run")?.Invoke(input, null) as Item ??
            throw new InvalidOperationException("The Method returned no Item.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
