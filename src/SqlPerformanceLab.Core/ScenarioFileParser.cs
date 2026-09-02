using System.Text;

namespace SqlPerformanceLab.Core;

public static class ScenarioFileParser
{
    private static readonly string[] ValidSections =
    [
        "setup",
        "bad",
        "good",
        "teardown"
    ];

    public static ScenarioDefinition Parse(string id, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        string? name = null;
        string? description = null;
        string? currentSection = null;

        var sections = ValidSections.ToDictionary(
            static x => x,
            static _ => new StringBuilder(),
            StringComparer.OrdinalIgnoreCase);

        foreach (string rawLine in content.Replace("\r\n", "\n").Split('\n'))
        {
            string line = rawLine.Trim();

            if (line.StartsWith("-- @scenario ", StringComparison.OrdinalIgnoreCase))
            {
                name = line["-- @scenario ".Length..].Trim();
                continue;
            }

            if (line.StartsWith("-- @description ", StringComparison.OrdinalIgnoreCase))
            {
                description = line["-- @description ".Length..].Trim();
                continue;
            }

            if (line.StartsWith("-- @", StringComparison.Ordinal))
            {
                string candidate = line[4..].Trim().ToLowerInvariant();

                if (sections.ContainsKey(candidate))
                {
                    currentSection = candidate;
                    continue;
                }
            }

            if (currentSection is not null)
            {
                sections[currentSection].AppendLine(rawLine);
            }
        }

        if (string.IsNullOrWhiteSpace(name))
            throw new FormatException($"Scenario '{id}' is missing '-- @scenario'.");

        if (string.IsNullOrWhiteSpace(description))
            throw new FormatException($"Scenario '{id}' is missing '-- @description'.");

        if (string.IsNullOrWhiteSpace(sections["bad"].ToString()))
            throw new FormatException($"Scenario '{id}' is missing a bad query section.");

        if (string.IsNullOrWhiteSpace(sections["good"].ToString()))
            throw new FormatException($"Scenario '{id}' is missing a good query section.");

        return new ScenarioDefinition(
            id,
            name,
            description,
            sections["setup"].ToString().Trim(),
            sections["bad"].ToString().Trim(),
            sections["good"].ToString().Trim(),
            sections["teardown"].ToString().Trim());
    }
}
