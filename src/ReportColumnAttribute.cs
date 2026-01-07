using System;

namespace BenchmarkDotNet.ReportColumns;

/// <summary>
/// Makes a property available as a report column in BenchmarkDotNet reports.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class ReportColumnAttribute : Attribute
{
    /// <summary>
    /// Gets the display name for the generated column.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, the generator derives the name from the property.
    /// </remarks>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets an optional unit label (e.g. <c>"B"</c>, <c>"ms"</c>)
    /// appended to the displayed value.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, no unit label is displayed.
    /// </remarks>
    public string? Unit { get; set; }

    /// <summary>
    /// Gets or sets the aggregation used to collapse multiple observed values
    /// into a single displayed value.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="Aggregation.Last"/>.
    /// </remarks>
    public Aggregation Aggregation { get; set; } = Aggregation.Last;
}
