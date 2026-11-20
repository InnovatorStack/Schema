using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace InnovatorSchema.HostTests;

internal static class MethodImports
{
    private static readonly CSharpParseOptions Options =
        new(LanguageVersion.CSharp6);

    public static SyntaxTree Apply(string source)
    {
        var root = CSharpSyntaxTree.ParseText(source, Options).GetCompilationUnitRoot();
        var imports = CSharpSyntaxTree.ParseText(
            "using InnovatorSchema;\nusing V = InnovatorSchema.Validator;\n", Options)
            .GetCompilationUnitRoot().Usings;

        foreach (var import in imports)
        {
            if (root.Usings.Any(item => SyntaxFactory.AreEquivalent(item, import)))
            {
                continue;
            }

            if (import.Alias is not null && root.Usings.Any(item =>
                item.Alias?.Name.Identifier.ValueText == import.Alias.Name.Identifier.ValueText))
            {
                throw new InvalidOperationException("The Method template already defines alias V.");
            }

            root = root.AddUsings(import);
        }

        return CSharpSyntaxTree.Create(root, Options);
    }

    public static void Check()
    {
        const string source = "using Aras.IOM; public class PrototypeMethod : Item {}";
        var first = Apply(source).GetRoot();
        var second = Apply(first.ToFullString()).GetRoot();

        if (!SyntaxFactory.AreEquivalent(first, second) || second.ContainsDiagnostics)
        {
            throw new InvalidOperationException("Method imports are not idempotent.");
        }

        try
        {
            Apply("using V = System.String;\n" + source);
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new InvalidOperationException("Conflicting template aliases must be rejected.");
    }
}
