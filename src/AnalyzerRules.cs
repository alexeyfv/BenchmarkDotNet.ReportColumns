using Microsoft.CodeAnalysis;

namespace BenchmarkDotNet.ReportColumns;

public static class AnalyzerRules
{
    public static readonly DiagnosticDescriptor TypeMustBePartialRule = new(
        id: "BDN1504",
        title: "Benchmark type must be partial",
        messageFormat: "Type '{0}' must be declared partial to use [ReportColumn] auto-registration",
        category: "ReportColumn",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PropertyMustBePublicInstanceGetter = new(
        id: "BDN1505",
        title: "[ReportColumn] property must be public instance getter",
        messageFormat: "Property '{0}' must be a public instance property with a public getter",
        category: "ReportColumn",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedPropertyType = new(
        id: "BDN1506",
        title: "Unsupported [ReportColumn] property type",
        messageFormat: "Property '{0}' has unsupported type '{1}'; supported: string, numeric primitives, System.TimeSpan or System.DateTime",
        category: "ReportColumn",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

}
