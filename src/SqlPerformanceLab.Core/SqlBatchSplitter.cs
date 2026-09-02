using System.Text;

namespace SqlPerformanceLab.Core;

public static class SqlBatchSplitter
{
    public static IReadOnlyList<string> Split(string script)
    {
        ArgumentNullException.ThrowIfNull(script);

        var batches = new List<string>();
        var current = new StringBuilder();

        foreach (string line in script.Replace("\r\n", "\n").Split('\n'))
        {
            if (string.Equals(line.Trim(), "GO", StringComparison.OrdinalIgnoreCase))
            {
                AddBatchIfPresent(batches, current);
                continue;
            }

            current.AppendLine(line);
        }

        AddBatchIfPresent(batches, current);
        return batches;
    }

    private static void AddBatchIfPresent(List<string> batches, StringBuilder current)
    {
        string batch = current.ToString().Trim();

        if (batch.Length > 0)
            batches.Add(batch);

        current.Clear();
    }
}
