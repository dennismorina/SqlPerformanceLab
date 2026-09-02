namespace SqlPerformanceLab.Core;

public sealed record BenchmarkMeasurement(
    double AverageMilliseconds,
    long AverageLogicalReads);
