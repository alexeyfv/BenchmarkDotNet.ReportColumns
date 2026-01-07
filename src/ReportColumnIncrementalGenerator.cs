using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BenchmarkDotNet.ReportColumns;

[Generator]
public sealed class ReportColumnIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var attributeName = typeof(ReportColumnAttribute).FullName;

        IncrementalValueProvider<ImmutableArray<ReportColumnPropertySpec>> collected = context
            .SyntaxProvider
            .ForAttributeWithMetadataName(attributeName,
                // We are only interested in properties
                predicate: (node, _) => node is PropertyDeclarationSyntax,
                // Transform from ReportColumnAttribute to ReportColumnEntry
                transform: (ctx, _) => Transform(ctx))
            // `Collect` is necessary to combine all the data into a single array,
            // so the sourrce generator can produce all the required code in one pass
            .Collect();

        context.RegisterSourceOutput(source: collected, SourceOutput);
    }

    private static void SourceOutput(SourceProductionContext context, ImmutableArray<ReportColumnPropertySpec> columnPropertySpecs)
    {
        if (columnPropertySpecs.IsDefaultOrEmpty)
        {
            return;
        }

        // We need only one instance of the aggregation helpers
        context.AddAggregationHelpersSource();

        // Group columns by containing type, so we can register one config and diagnoser per type 
        var groupedColumns = columnPropertySpecs.GroupBy(spec => spec.Property.ContainingType, SymbolEqualityComparer.Default);

        foreach (var group in groupedColumns)
        {
            if (group.Key is not INamedTypeSymbol containingType)
            {
                // This should never happen, but just in case
                throw new InvalidOperationException("Containing type is not a named type symbol");
            }

            var benchmarkSpec = new BenchmarkTypeSpec(containingType);

            if (!benchmarkSpec.TryValidate(out var benchmarkDiagnostics))
            {
                foreach (var diag in benchmarkDiagnostics)
                {
                    context.ReportDiagnostic(diag);
                }

                continue;
            }

            var validColumnPropertySpecs = new List<ReportColumnPropertySpec>();

            foreach (ReportColumnPropertySpec col in group)
            {
                if (!col.TryValidate(out var columnDiagnostics))
                {
                    foreach (var diag in columnDiagnostics)
                    {
                        context.ReportDiagnostic(diag);
                    }

                    continue;
                }

                validColumnPropertySpecs.Add(col);
            }

            if (validColumnPropertySpecs.Count == 0)
            {
                continue;
            }

            // For each benchmark type, generate the diagnoser, diagnoser handler, columns, config, registrar, and results classes
            context.AddInProcessDiagnoser(benchmarkSpec);
            context.AddInProcessDiagnoserHandler(benchmarkSpec, validColumnPropertySpecs);
            context.AddColumns(benchmarkSpec, validColumnPropertySpecs);
            context.AddConfig(benchmarkSpec, validColumnPropertySpecs);
            context.AddRegistrar(benchmarkSpec);
            context.AddResults(benchmarkSpec);
        }
    }

    private static ReportColumnPropertySpec Transform(GeneratorAttributeSyntaxContext ctx)
    {
        // We know the target is a property because of the predicate in ForAttributeWithMetadataName
        var property = (IPropertySymbol)ctx.TargetSymbol;

        // We know there is at least one attribute because of ForAttributeWithMetadataName
        var attribute = ctx.Attributes.First();

        return new ReportColumnPropertySpec(property, attribute);
    }
}
