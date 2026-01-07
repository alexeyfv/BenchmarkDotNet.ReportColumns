using System.Reflection;
using Xunit;

namespace BenchmarkDotNet.ReportColumns.Tests;

public sealed class ReportColumnGeneratorTests
{
    [Fact]
    public void Generator_Generates_Config_Diagnoser_And_Columns_For_Annotated_Property()
    {
        // Arrange
        var code =
        """
        using BenchmarkDotNet.Attributes;
        using BenchmarkDotNet.ReportColumns;

        namespace Test;

        public partial class MyBenchmark
        {
            [Benchmark]
            public void Work() { }

            [ReportColumn(Name = "Bytes", Unit = "B", Aggregation = Aggregation.Mean)]
            public long BytesWritten => 123;
        }
        """;

        // Act
        var result = GeneratorTestHarness.Run(code, assemblyName: "GeneratorSmokeTest");

        // Assert
        GeneratorTestHarness.AssertNoErrors(result);

        Assert.Contains("MyBenchmark_ManualConfig", result.GeneratedText);
        Assert.Contains("IInProcessDiagnoser", result.GeneratedText);
        Assert.Contains("IColumn", result.GeneratedText);
        Assert.Contains("[Config(typeof(", result.GeneratedText);
        Assert.Contains("BytesWritten", result.GeneratedText);
        Assert.Contains("Bytes [B]", result.GeneratedText);
    }


    public static TheoryData<string, string> ColumnHeaderCases => new()
    {
        { @"ReportColumn", "BytesWritten" },
        { @"ReportColumn(Unit = ""ms"")", "BytesWritten [ms]" },
        { @"ReportColumn(Name = ""Time"")", "Time" },
        { @"ReportColumn(Name = ""Time"", Unit = ""ms"")", "Time [ms]" },
    };

    [Theory]
    [MemberData(nameof(ColumnHeaderCases))]
    public void ColumnHeader_UsesNameOrPropertyName_AndAppendsUnit(string attribute, string expectedHeader)
    {
        // Arrange
        var template =
        """
        using BenchmarkDotNet.Attributes;
        using BenchmarkDotNet.ReportColumns;

        namespace Test;

        public partial class MyBenchmark
        {{
            [Benchmark]
            public void Work() {{ }}

            [{0}]
            public long BytesWritten => 123;
        }}
        """;

        var code = string.Format(template, attribute);

        // Act
        var result = GeneratorTestHarness.Run(code);

        // Assert
        GeneratorTestHarness.AssertNoErrors(result);
        Assert.Contains(@$"public string ColumnName => ""{expectedHeader}"";", result.GeneratedText);
    }

    public static TheoryData<Aggregation, string> AggregationCases => new()
    {
        { Aggregation.First, "4" },
        { Aggregation.Last, "2" },
        { Aggregation.Min, "1" },
        { Aggregation.Max, "5" },
        { Aggregation.Mean, "3" },
        { Aggregation.Median, "3" },
    };

    [Theory]
    [MemberData(nameof(AggregationCases))]
    public void AggregationHelpers_ComputesExpectedValue(Aggregation aggregation, string expected)
    {
        // Arrange
        var code =
        """
        using BenchmarkDotNet.Attributes;
        using BenchmarkDotNet.ReportColumns;

        namespace Test;

        public partial class MyBenchmark
        {
            [Benchmark]
            public void Work() { }

            [ReportColumn]
            public int Value => 0;
        }
        """;

        var result = GeneratorTestHarness.Run(code);
        GeneratorTestHarness.AssertNoErrors(result);

        var assembly = GeneratorTestHarness.EmitAndLoad(result.UpdatedCompilation);
        var helpersType = assembly.GetType("BenchmarkDotNet.ReportColumn.Generated.AggregationHelpers", throwOnError: true)!;

        var list = new List<int> { 4, 5, 1, 3, 2 };

        // Act
        var aggregateMethod = helpersType
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Single(m => m.Name == "Aggregate" && m.IsGenericMethodDefinition);

        var closed = aggregateMethod.MakeGenericMethod(typeof(int));
        var actual = (string)closed.Invoke(null, [aggregation, list])!;

        // Assert
        Assert.Equal(expected, actual);
    }

    public static TheoryData<string, string> SupportedTypeCases => new()
    {
        { "bool", "true" },
        { "sbyte", "(sbyte)1" },
        { "short", "(short)2" },
        { "int", "3" },
        { "long", "4L" },
        { "byte", "(byte)5" },
        { "ushort", "(ushort)6" },
        { "uint", "7u" },
        { "ulong", "8ul" },
        { "float", "1.5f" },
        { "double", "2.5" },
        { "decimal", "3.5m" },
        { "char", "'x'" },
        { "string", @"""hello""" },
        { "System.DateTime", "new System.DateTime(2026, 1, 1)" },
        { "System.TimeSpan", "System.TimeSpan.FromMilliseconds(123)" },
    };

    [Theory]
    [MemberData(nameof(SupportedTypeCases))]
    public void SupportedPropertyTypes_DoNotProduceDiagnostics(string typeName, string expression)
    {
        // Arrange
        var template =
        """
        using BenchmarkDotNet.Attributes;
        using BenchmarkDotNet.ReportColumns;

        namespace Test;

        public partial class MyBenchmark
        {{
            [Benchmark]
            public void Work() {{ }}

            [ReportColumn]
            public {0} Value => {1};
        }}
        """;

        var code = string.Format(template, typeName, expression);

        // Act
        var result = GeneratorTestHarness.Run(code);

        // Assert
        GeneratorTestHarness.AssertNoErrors(result);
    }
}
