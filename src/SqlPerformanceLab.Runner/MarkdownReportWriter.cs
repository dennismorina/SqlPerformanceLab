using System.Globalization;
using System.Text;
using SqlPerformanceLab.Core;

namespace SqlPerformanceLab.Runner;

internal static class MarkdownReportWriter
{
    public static async Task WriteAsync(
        string path,
        IReadOnlyCollection<ScenarioResult> results)
    {
        string? directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var builder = new StringBuilder();
        builder.AppendLine("# SqlPerformanceLab benchmark results");
        builder.AppendLine();
        builder.AppendLine($"Generated: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        builder.AppendLine();
        builder.AppendLine("| Scenario | Bad avg ms | Good avg ms | Speedup | Bad reads | Good reads | Read reduction |");
        builder.AppendLine("|---|---:|---:|---:|---:|---:|---:|");

        foreach (ScenarioResult result in results)
        {
            builder.AppendLine(
                $"| {Escape(result.Scenario.Name)} | " +
                $"{result.Bad.AverageMilliseconds.ToString("F2", CultureInfo.InvariantCulture)} | " +
                $"{result.Good.AverageMilliseconds.ToString("F2", CultureInfo.InvariantCulture)} | " +
                $"{result.Speedup.ToString("F2", CultureInfo.InvariantCulture)}x | " +
                $"{result.Bad.AverageLogicalReads} | " +
                $"{result.Good.AverageLogicalReads} | " +
                $"{result.ReadReductionPercent.ToString("F1", CultureInfo.InvariantCulture)}% |");
        }

        await File.WriteAllTextAsync(path, builder.ToString());
    }

    private static string Escape(string value) => value.Replace("|", "\\|");
}
