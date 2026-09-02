using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using SqlPerformanceLab.Core;

namespace SqlPerformanceLab.Runner;

internal sealed partial class BenchmarkRunner(string connectionString)
{
    public async Task<ScenarioResult> RunAsync(
        ScenarioDefinition scenario,
        int iterations,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(scenario.SetupSql))
        {
            await SqlScriptExecutor.ExecuteScriptAsync(
                connectionString,
                scenario.SetupSql,
                cancellationToken);
        }

        try
        {
            await ExecuteOnceAsync(scenario.BadSql, cancellationToken);
            await ExecuteOnceAsync(scenario.GoodSql, cancellationToken);

            BenchmarkMeasurement bad =
                await MeasureAsync(scenario.BadSql, iterations, cancellationToken);

            BenchmarkMeasurement good =
                await MeasureAsync(scenario.GoodSql, iterations, cancellationToken);

            return new ScenarioResult(scenario, bad, good);
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(scenario.TeardownSql))
            {
                await SqlScriptExecutor.ExecuteScriptAsync(
                    connectionString,
                    scenario.TeardownSql,
                    cancellationToken);
            }
        }
    }

    private async Task<BenchmarkMeasurement> MeasureAsync(
        string sql,
        int iterations,
        CancellationToken cancellationToken)
    {
        var elapsed = new List<double>(iterations);
        var reads = new List<long>(iterations);

        for (int i = 0; i < iterations; i++)
        {
            (double ms, long logicalReads) = await ExecuteOnceAsync(sql, cancellationToken);
            elapsed.Add(ms);
            reads.Add(logicalReads);
        }

        return new BenchmarkMeasurement(
            elapsed.Average(),
            (long)Math.Round(reads.Average()));
    }

    private async Task<(double Milliseconds, long LogicalReads)> ExecuteOnceAsync(
        string sql,
        CancellationToken cancellationToken)
    {
        long logicalReads = 0;

        await using SqlConnection connection =
            await SqlConnectionFactory.OpenWithRetryAsync(connectionString, cancellationToken);

        connection.FireInfoMessageEventOnUserErrors = true;
        connection.InfoMessage += (_, e) =>
        {
            foreach (SqlError error in e.Errors)
            {
                foreach (Match match in LogicalReadsRegex().Matches(error.Message))
                {
                    if (long.TryParse(match.Groups[1].Value, out long value))
                        logicalReads += value;
                }
            }
        };

        await using var command = new SqlCommand(
            "SET NOCOUNT ON; SET STATISTICS IO ON;" + Environment.NewLine + sql,
            connection)
        {
            CommandTimeout = 180
        };

        var stopwatch = Stopwatch.StartNew();

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        do
        {
            while (await reader.ReadAsync(cancellationToken))
            {
            }
        }
        while (await reader.NextResultAsync(cancellationToken));

        stopwatch.Stop();

        return (stopwatch.Elapsed.TotalMilliseconds, logicalReads);
    }

    [GeneratedRegex(@"logical reads\s+(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LogicalReadsRegex();
}
