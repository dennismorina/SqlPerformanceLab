namespace SqlPerformanceLab.Core;

public sealed record ScenarioResult(
    ScenarioDefinition Scenario,
    BenchmarkMeasurement Bad,
    BenchmarkMeasurement Good)
{
    public double Speedup =>
        Good.AverageMilliseconds <= 0
            ? 0
            : Bad.AverageMilliseconds / Good.AverageMilliseconds;

    public double ReadReductionPercent =>
        Bad.AverageLogicalReads <= 0
            ? 0
            : (1d - ((double)Good.AverageLogicalReads / Bad.AverageLogicalReads)) * 100d;
}
