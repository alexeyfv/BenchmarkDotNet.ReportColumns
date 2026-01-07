using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace BenchmarkDotNet.ReportColumns.Tests;

internal static class GeneratorTestHarness
{
    internal sealed record Result(
        CSharpCompilation UpdatedCompilation,
        IReadOnlyList<Diagnostic> GeneratorDiagnostics,
        string GeneratedText);

    public static Result Run(string code, string assemblyName = "GeneratorTest")
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);

        var references = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToList();

        // Ensure required assemblies are included even if not already loaded.
        references.Add(MetadataReference.CreateFromFile(typeof(ReportColumnAttribute).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(Attributes.BenchmarkAttribute).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(Attributes.ConfigAttribute).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(Mathematics.Statistics).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Text.Json.JsonSerializer).Assembly.Location));

        // BenchmarkDotNet.Mathematics depends on Pragmastat
        var pragmastatSample = Type.GetType("Pragmastat.Sample, Pragmastat", throwOnError: true)!;
        references.Add(MetadataReference.CreateFromFile(pragmastatSample.Assembly.Location));

        var compilation = CSharpCompilation.Create(
            assemblyName: assemblyName,
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new ReportColumnIncrementalGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generators: new[] { generator.AsSourceGenerator() });

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var generatorDiagnostics);

        var runResult = driver.GetRunResult();
        var generatedText = string.Join("\n\n", runResult.GeneratedTrees.Select(t => t.GetText().ToString()));

        return new Result((CSharpCompilation)updatedCompilation, generatorDiagnostics, generatedText);
    }

    public static Assembly EmitAndLoad(CSharpCompilation compilation)
    {
        using var peStream = new MemoryStream();
        var emitResult = compilation.Emit(peStream);
        Assert.True(emitResult.Success);

        peStream.Position = 0;
        return Assembly.Load(peStream.ToArray());
    }

    public static void AssertNoErrors(Result result)
    {
        var compileDiagnostics = result.UpdatedCompilation.GetDiagnostics();
        Assert.DoesNotContain(result.GeneratorDiagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(compileDiagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }
}
