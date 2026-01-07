namespace BenchmarkDotNet.ReportColumns;

/// <summary>
/// Defines how a sequence of observed values for a custom column should be aggregated
/// into the single value that is displayed in the BenchmarkDotNet summary table.
/// </summary>
public enum Aggregation
{
    /// <summary>
    /// Uses the first observed value.
    /// </summary>
    First = 0,

    /// <summary>
    /// Uses the last observed value.
    /// </summary>
    Last = 1,

    /// <summary>
    /// Uses the minimum observed value.
    /// </summary>
    Min = 3,

    /// <summary>
    /// Uses the maximum observed value.
    /// </summary>
    Max = 4,

    /// <summary>
    /// Uses the arithmetic mean (average) of all observed values.
    /// </summary>
    Mean = 5,

    /// <summary>
    /// Uses the median of all observed values.
    /// </summary>
    Median = 6,
}
