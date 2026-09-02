using SqlPerformanceLab.Core;

namespace SqlPerformanceLab.Runner;

internal sealed class ScriptCatalog
{
    private readonly string _sqlRoot;

    public ScriptCatalog()
    {
        _sqlRoot = Path.Combine(AppContext.BaseDirectory, "sql");

        if (!Directory.Exists(_sqlRoot))
            throw new DirectoryNotFoundException($"SQL directory not found: {_sqlRoot}");
    }

    public string ReadSetupScript(string fileName)
    {
        string path = Path.Combine(_sqlRoot, "setup", fileName);
        return File.ReadAllText(path);
    }

    public IReadOnlyList<ScenarioDefinition> LoadScenarios()
    {
        string directory = Path.Combine(_sqlRoot, "scenarios");

        return Directory
            .GetFiles(directory, "*.sql")
            .OrderBy(static x => x, StringComparer.OrdinalIgnoreCase)
            .Select(path =>
            {
                string id = Path.GetFileNameWithoutExtension(path);
                string content = File.ReadAllText(path);
                return ScenarioFileParser.Parse(id, content);
            })
            .ToArray();
    }
}
