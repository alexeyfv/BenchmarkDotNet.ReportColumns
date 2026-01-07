using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.ReportColumns;

namespace Demo;

public partial class MyBenchmark
{
    [ReportColumn]
    public int MyAwesomeColumn { get; set; }

    [Benchmark]
    public async Task BenchmarkMethod()
    {
        MyAwesomeColumn = Random.Shared.Next(1, 100);
        await Task.Delay(1000);
    }
}