using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace BenchmarkDotNet.ReportColumns;

internal sealed class ReportColumnPropertySpec(IPropertySymbol property, AttributeData attribute)
{
    public IPropertySymbol Property => property;

    public AttributeData Attribute => attribute;

    public string ColumnName => GetByName("Name") as string ?? Property.Name;

    public string ColumnUnit => GetByName("Unit") as string ?? string.Empty;

    public string ColumnHeader => string.IsNullOrEmpty(ColumnUnit) ? ColumnName : $"{ColumnName} [{ColumnUnit}]";

    public Aggregation Aggregation => GetByName("Aggregation") is int aggValue ? (Aggregation)aggValue : Aggregation.Last;

    public string PropertyName { get; } = property.Name;

    // Multiple benchmark types can have properties with the same name.
    // So we need to disambiguate them when generating column types.
    public string SafeColumnTypeName => $"{property.ContainingType.Name}_{property.Name}";

    /// <summary>
    /// A stable key used for storing aggregated values produced by the in-process handler.
    /// </summary>
    public string ResultKey => $"Samples_{PropertyName}";

    /// <summary>
    /// Whether the column represents a numeric value.
    /// </summary>
    public bool IsNumeric => (Property.Type.ContainingNamespace.ContainingNamespace.MetadataName, Property.Type.MetadataName) switch
    {
        ("System", "SByte") => true,
        ("System", "Int16") => true,
        ("System", "Int32") => true,
        ("System", "Int64") => true,

        ("System", "Byte") => true,
        ("System", "UInt16") => true,
        ("System", "UInt32") => true,
        ("System", "UInt64") => true,

        ("System", "Single") => true,
        ("System", "Double") => true,
        ("System", "Decimal") => true,

        _ => false,
    };

    /// <summary>
    /// C# type name suitable for generated source (e.g. for <c>List&lt;T&gt;</c> fields).
    /// </summary>
    /// <remarks>
    /// Returns keyword aliases where available; falls back to a fully-qualified name.
    /// </remarks>
    public string CSharpTypeName => Property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private object? GetByName(string name)
    {
        foreach (var kv in Attribute.NamedArguments)
        {
            if (string.Equals(kv.Key, name, StringComparison.Ordinal))
            {
                return kv.Value.Value;
            }
        }

        return default;
    }

    public bool TryValidate(out IReadOnlyList<Diagnostic> diagnostics)
    {
        var list = new List<Diagnostic>();

        // Validate property accessibility
        var isAccessible =
            !Property.IsStatic &&
            Property.DeclaredAccessibility == Accessibility.Public &&
            Property.GetMethod is not null &&
            Property.GetMethod.DeclaredAccessibility == Accessibility.Public;

        if (!isAccessible)
        {
            list.Add(Diagnostic.Create(AnalyzerRules.PropertyMustBePublicInstanceGetter, Property.Locations.FirstOrDefault(), Property.ToDisplayString()));
        }

        // Validate property type
        var isValidType = (Property.Type.ContainingNamespace.MetadataName, Property.Type.MetadataName) switch
        {
            ("System", "Boolean") => true,

            ("System", "SByte") => true,
            ("System", "Int16") => true,
            ("System", "Int32") => true,
            ("System", "Int64") => true,

            ("System", "Byte") => true,
            ("System", "UInt16") => true,
            ("System", "UInt32") => true,
            ("System", "UInt64") => true,

            ("System", "Single") => true,
            ("System", "Double") => true,
            ("System", "Decimal") => true,

            ("System", "Char") => true,
            ("System", "String") => true,

            ("System", "DateTime") => true,
            ("System", "TimeSpan") => true,

            _ => false,
        };

        if (!isValidType)
        {
            list.Add(Diagnostic.Create(AnalyzerRules.UnsupportedPropertyType, Property.Locations.FirstOrDefault(), Property.ToDisplayString(), $"{Property.Type.ContainingNamespace.MetadataName}.{Property.Type.MetadataName}"));
        }

        diagnostics = list;
        return diagnostics.Count == 0;
    }
}
