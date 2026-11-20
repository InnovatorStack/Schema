using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace InnovatorSchema.HostTests;

internal static class MethodCompiler
{
    private static readonly string[] MethodFiles =
        { "PartMethod.cs", "JsonMethod.cs", "AmlMethod.cs", "TypedOutputMethod.cs",
            "ParsePartMethod.cs", "ParsePartsMethod.cs", "AssemblyMethod.cs" };
    private static readonly string[] ServerFiles =
        { "ValidateJson.cs", "ValidateAml.cs", "ValidateXml.cs", "ParseValues.cs" };

    public static void Check(string serverBin, string? configPath = null)
    {
        CheckProfile(serverBin);
        configPath ??= Path.Combine(serverBin, "..", "method-config.xml");
        var config = XDocument.Load(configPath);
        string template = config.Descendants("Template")
            .Single(element => (string?)element.Attribute("name") == "CSharp").Value;
        foreach (string file in MethodFiles)
        {
            string method = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, file));
            CheckMethod(template, method, References(serverBin, config));
        }

        foreach (string file in ServerFiles)
        {
            string method = File.ReadAllText(
                Path.Combine(AppContext.BaseDirectory, "ServerMethods", file));
            CheckMethod(template, method, References(serverBin, config));
        }
    }

    private static void CheckMethod(
        string template, string method, IEnumerable<MetadataReference> references)
    {
        string source = template
            .Replace("$(pkgname)", "InnovatorSchemaMethodCheck", StringComparison.Ordinal)
            .Replace("$(clsname)", "MethodCheck", StringComparison.Ordinal)
            .Replace("$(interfacename)", "Aras.Server.Core.IInnovatorServerMethod",
                StringComparison.Ordinal)
            .Replace("$(fncname)", "FNCMethod", StringComparison.Ordinal)
            .Replace("$(MethodCode)", method, StringComparison.Ordinal);
        var tree = MethodImports.Apply(source);
        var compilation = CSharpCompilation.Create("InnovatorSchemaMethodCheck",
            new[] { tree }, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var output = new MemoryStream();
        var result = compilation.Emit(output);

        if (!result.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine,
                result.Diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error)));
        }
    }

    private static IEnumerable<MetadataReference> References(string serverBin, XDocument config)
    {
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is not string runtime)
        {
            throw new InvalidOperationException("Runtime references are unavailable.");
        }

        var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string core = Path.GetDirectoryName(typeof(object).Assembly.Location) ??
            throw new InvalidOperationException("The runtime directory is unavailable.");
        string shared = Directory.GetParent(core)?.Parent?.FullName ??
            throw new InvalidOperationException("The shared framework directory is unavailable.");

        foreach (string path in runtime.Split(Path.PathSeparator))
        {
            if (path.StartsWith(shared + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
            {
                paths[Path.GetFileName(path)] = path;
            }
        }

        foreach (var reference in config.Descendants("ReferencedAssemblies").Elements("name"))
        {
            string name = reference.Value.Replace("$(binpath)/", "", StringComparison.Ordinal);
            string path = Path.Combine(serverBin, name);

            if (File.Exists(path))
            {
                paths[name] = path;
            }
            else if (!paths.ContainsKey(name))
            {
                throw new FileNotFoundException("A configured Method reference is missing.", path);
            }
        }

        paths["InnovatorSchema.dll"] = typeof(Schema<string>).Assembly.Location;

        if (paths.Count > 1024)
        {
            throw new InvalidOperationException("Method compilation reference limit exceeded.");
        }

        return paths.Values.Select(path => MetadataReference.CreateFromFile(path));
    }

    private static void CheckProfile(string serverBin)
    {
        var assemblies = new[] { typeof(global::Aras.IOM.Item).Assembly,
            typeof(Compilation).Assembly, typeof(CSharpCompilation).Assembly };

        foreach (var assembly in assemblies)
        {
            string expected = Path.Combine(serverBin, Path.GetFileName(assembly.Location));
            byte[] loadedHash = SHA256.HashData(File.ReadAllBytes(assembly.Location));
            byte[] expectedHash = SHA256.HashData(File.ReadAllBytes(expected));

            if (!loadedHash.SequenceEqual(expectedHash))
            {
                throw new InvalidOperationException("Loaded assembly differs from the host: " +
                    assembly.GetName().Name);
            }
        }
    }
}
