# BenchmarkDotNet.ReportColumns

Source generator that makes it easy to add custom columns to BenchmarkDotNet summaries by annotating properties with `[ReportColumn]`.

## Basic usage

### 1. Add BenchmarkDotNet nightly feed

This library relies on BenchmarkDotNet functionality that isn’t released yet, so you need to add the BenchmarkDotNet nightly feed to the NuGet sources.

Create a `nuget.config` file in your solution root with this content:

``` xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
<packageSources>
    <!-- Official NuGet feed -->
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
    <!-- BenchmarkDotNet nightly feed -->
    <add key="bdn-nightly" value="https://www.myget.org/F/benchmarkdotnet/api/v3/index.json" />
</packageSources>
</configuration>
```

### 2. Install packages

Add BenchmarkDotNet nightly package and source generator package to your benchmark project:

```bash
dotnet add package BenchmarkDotNet --version 0.16.0-nightly.20260105.397 
dotnet add package AlekseiFedorov.BenchmarkDotNet.ReportColumns
```

### 3. Annotate properties

Make your benchmark class `partial` and mark any properties you want to appear as report columns with `[ReportColumn]`:

``` cs
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
```

By default, the generator will create a column with the property name as the header and will use the `Last` observed value as the column value.

``` markdown
| Method          |    Mean |    Error |   StdDev | MyAwesomeColumn |
| --------------- | ------: | -------: | -------: | --------------- |
| BenchmarkMethod | 1.000 s | 0.0001 s | 0.0001 s | 77              |
```

## Constraints

- Benchmark type must be `partial`.
- Column property must be `public`, non-static and with a `public` getter.
- Supported property types: `bool`, `char`, numeric primitives, `string`, `DateTime`, `TimeSpan`.

## Available options

The `[ReportColumn]` attribute supports the following optional parameters:

- `Name`: overrides the column header (default is the property name).
- `Unit`: appended to the header as `[Unit]`.
- `Aggregation`: how multiple observed values are reduced to one value. Available aggregation options: `First`, `Last`, `Min`, `Max`, `Mean`, `Median`.

Example:

``` cs
public partial class MyBenchmark
{
    [ReportColumn(Name = "Awesome Column", Unit = "ms", Aggregation = Aggregation.Mean)]
    public int MyAwesomeColumn { get; set; }
}
```

Results in:

``` markdown
| Method          |    Mean |    Error |   StdDev | Awesome Column [ms] |
| --------------- | ------: | -------: | -------: | ------------------- |
| BenchmarkMethod | 1.000 s | 0.0001 s | 0.0001 s | 20                  |
```
