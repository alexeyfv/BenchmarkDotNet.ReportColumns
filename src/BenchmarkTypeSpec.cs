using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BenchmarkDotNet.ReportColumns;

internal readonly struct BenchmarkTypeSpec(INamedTypeSymbol containingType)
{
    private readonly INamedTypeSymbol ContainingType { get; } = containingType;
    private string SafeNamespace
    {
        get
        {
            // There can be types with the same name in different namespaces.
            // So we generate a stable hash based on the namespace to avoid name collisions.
            // And the length of the generated name should be limited to avoid issues with long names.
            // So MD5 is sufficient here.
            using var algorithm = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(ContainingType.ContainingNamespace.MetadataName);
            var hashBytes = algorithm.ComputeHash(bytes);
            var hash = Convert.ToBase64String(hashBytes)
                .Replace("+", string.Empty)
                .Replace("/", string.Empty)
                .Replace("=", string.Empty);

            return $"N{hash}";
        }
    }

    public string Namespace { get; } = containingType.ContainingNamespace.ToDisplayString();
    public string GeneratedNamespace => $"BenchmarkDotNet.ReportColumns.Generated.{SafeNamespace}";

    public string TypeName { get; } = containingType.Name;
    public string SafeBenchmarkTypeName => $"{SafeNamespace}_{ContainingType.Name}";

    public string ResultsTypeName => TypeName + "_Results";
    public string InProcessDiagnoserTypeName => TypeName + "_InProcessDiagnoser";
    public string InProcessDiagnoserHandlerTypeName => TypeName + "_InProcessDiagnoserHandler";
    public string ManualConfigTypeName => TypeName + "_ManualConfig";

    public bool TryValidate(out IReadOnlyList<Diagnostic> diagnostics)
    {
        var list = new List<Diagnostic>();

        // Validate if the benchmark type is partial
        // to allow to add ManualConfig attribute on the type automatically
        foreach (var syntaxRef in ContainingType.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is TypeDeclarationSyntax tds)
            {
                if (!tds.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    // Type is not partial
                    list.Add(Diagnostic.Create(AnalyzerRules.TypeMustBePartialRule, ContainingType.Locations.FirstOrDefault(), TypeName));
                    break;
                }
            }
        }

        diagnostics = list;
        return diagnostics.Count == 0;
    }
}
