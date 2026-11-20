using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Security.Cryptography;
using Aras.IOM;
using InnovatorSchema.Testing;

namespace InnovatorSchema.HostTests;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 3 && args[0] == "--check-template")
            {
                ResolveFrom(args[1]);
                PrintIdentity();
                Run("Candidate CSharp Method template (compile only)",
                    () => MethodCompiler.Check(args[1], args[2]));
                return 0;
            }

            if (args.Length == 2 && args[0] == "--export-server-cases")
            {
                ServerMethodChecks.Export(args[1]);
                return 0;
            }

            if (args.Length == 1 && args[0] == "--server-cases-offline")
            {
                Run("Server payload suite (offline only)", ServerMethodChecks.Check);
                return 0;
            }

            if (args.Length == 1)
            {
                ResolveFrom(args[0]);
            }

            PrintIdentity();
            Run("Nested typed Item output", ArasScenarios.NestedItemOutput);
            Run("Relationship paths and IOM errors", ArasScenarios.RelationshipFailureAndErrors);
            Run("Cardinality and expansion", ArasScenarios.CardinalityAndExpansion);
            Run("Reusable definitions", ArasScenarios.ReusableSchemas);
            Run("Example Method bodies (offline Item wrapper)", MethodBodyChecks.Check);
            Run("Server payload suite (offline Method wrappers)", ServerMethodChecks.Check);

            if (args.Length == 1)
            {
                Run("Installed CSharp Method template (compile only)",
                    () => MethodCompiler.Check(args[0]));
            }
            else if (args.Length != 0)
            {
                throw new ArgumentException(
                    "Supply only the server bin directory, or no arguments.");
            }

            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void Run(string name, Action check)
    {
        check();
        Console.WriteLine("PASS " + name);
    }

    private static void PrintIdentity()
    {
        Console.WriteLine(RuntimeInformation.FrameworkDescription);
        var assembly = typeof(Item).Assembly;
        Console.WriteLine(assembly.FullName);
        Console.WriteLine("IOM SHA256 " +
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location))));
        Console.WriteLine("Compiler " +
            typeof(Microsoft.CodeAnalysis.Compilation).Assembly.FullName);
        Console.WriteLine("Compiler SHA256 " + Convert.ToHexString(SHA256.HashData(
            File.ReadAllBytes(typeof(Microsoft.CodeAnalysis.Compilation).Assembly.Location))));
    }

    private static void ResolveFrom(string directory)
    {
        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            string path = Path.Combine(directory, name.Name + ".dll");

            if (File.Exists(path))
            {
                return context.LoadFromAssemblyPath(Path.GetFullPath(path));
            }

            return null;
        };
    }
}
