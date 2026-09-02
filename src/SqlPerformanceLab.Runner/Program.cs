using SqlPerformanceLab.Core;
using SqlPerformanceLab.Runner;

try
{
    CliOptions options = CliOptions.Parse(args);
    var catalog = new ScriptCatalog();

    if (options.Command is "help")
    {
        PrintHelp();
        return 0;
    }

    if (options.Command is "list")
    {
        PrintScenarios(catalog.LoadScenarios());
        return 0;
    }

    string connectionString =
        SqlConnectionFactory.ResolveConnectionString(options.ConnectionString);

    switch (options.Command)
    {
        case "setup":
            await SetupAsync(catalog, connectionString);
            return 0;

        case "run":
            return await RunAsync(catalog, connectionString, options);

        default:
            Console.Error.WriteLine($"Unknown command '{options.Command}'.");
            PrintHelp();
            return 2;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine($"ERROR: {ex.Message}");
    return 1;
}

static async Task SetupAsync(ScriptCatalog catalog, string masterConnection)
{
    Console.WriteLine("SqlPerformanceLab setup");
    Console.WriteLine("-----------------------");
    Console.WriteLine("Waiting for SQL Server...");

    await using (var connection =
        await SqlConnectionFactory.OpenWithRetryAsync(masterConnection))
    {
        Console.WriteLine($"Connected to SQL Server {connection.ServerVersion}.");
    }

    Console.WriteLine("Creating database...");
    await SqlScriptExecutor.ExecuteScriptAsync(
        SqlConnectionFactory.WithDatabase(masterConnection, "master"),
        catalog.ReadSetupScript("00_create_database.sql"));

    string labConnection =
        SqlConnectionFactory.WithDatabase(masterConnection, "SqlPerformanceLab");

    Console.WriteLine("Creating schema...");
    await SqlScriptExecutor.ExecuteScriptAsync(
        labConnection,
        catalog.ReadSetupScript("01_schema.sql"));

    Console.WriteLine("Seeding deterministic benchmark data...");
    await SqlScriptExecutor.ExecuteScriptAsync(
        labConnection,
        catalog.ReadSetupScript("02_seed.sql"));

    Console.WriteLine("Setup complete.");
    Console.WriteLine("Customers: 50,000");
    Console.WriteLine("Orders:    250,000");
}

static async Task<int> RunAsync(
    ScriptCatalog catalog,
    string masterConnection,
    CliOptions options)
{
    IReadOnlyList<ScenarioDefinition> all = catalog.LoadScenarios();
    IReadOnlyList<ScenarioDefinition> selected = SelectScenarios(all, options.Scenario);

    if (selected.Count == 0)
    {
        Console.Error.WriteLine("No matching scenario found.");
        PrintScenarios(all);
        return 2;
    }

    string labConnection =
        SqlConnectionFactory.WithDatabase(masterConnection, "SqlPerformanceLab");

    var runner = new BenchmarkRunner(labConnection);
    var results = new List<ScenarioResult>();

    Console.WriteLine("SqlPerformanceLab");
    Console.WriteLine("-----------------");
    Console.WriteLine($"Iterations: {options.Iterations}");
    Console.WriteLine();

    foreach (ScenarioDefinition scenario in selected)
    {
        Console.WriteLine($"[{scenario.Id}] {scenario.Name}");
        Console.WriteLine(scenario.Description);

        ScenarioResult result =
            await runner.RunAsync(scenario, options.Iterations);

        results.Add(result);

        Console.WriteLine(
            $"  BAD : {result.Bad.AverageMilliseconds,8:F2} ms | " +
            $"{result.Bad.AverageLogicalReads,10:N0} logical reads");

        Console.WriteLine(
            $"  GOOD: {result.Good.AverageMilliseconds,8:F2} ms | " +
            $"{result.Good.AverageLogicalReads,10:N0} logical reads");

        Console.WriteLine(
            $"  Gain: {result.Speedup:F2}x faster | " +
            $"{result.ReadReductionPercent:F1}% fewer logical reads");

        Console.WriteLine();
    }

    if (!string.IsNullOrWhiteSpace(options.OutputPath))
    {
        await MarkdownReportWriter.WriteAsync(options.OutputPath, results);
        Console.WriteLine($"Report: {options.OutputPath}");
    }

    return 0;
}

static IReadOnlyList<ScenarioDefinition> SelectScenarios(
    IReadOnlyList<ScenarioDefinition> all,
    string? selector)
{
    if (string.IsNullOrWhiteSpace(selector) ||
        string.Equals(selector, "all", StringComparison.OrdinalIgnoreCase))
    {
        return all;
    }

    return all
        .Where(x =>
            x.Id.Contains(selector, StringComparison.OrdinalIgnoreCase) ||
            x.Name.Contains(selector, StringComparison.OrdinalIgnoreCase))
        .ToArray();
}

static void PrintScenarios(IReadOnlyList<ScenarioDefinition> scenarios)
{
    Console.WriteLine("Available scenarios:");

    foreach (ScenarioDefinition scenario in scenarios)
        Console.WriteLine($"  {scenario.Id,-28} {scenario.Name}");
}

static void PrintHelp()
{
    Console.WriteLine("""
SqlPerformanceLab

Commands:
  setup
      Creates/recreates the benchmark schema and seeds test data.

  list
      Lists all benchmark scenarios.

  run [options]
      Runs one or all performance scenarios.

Run options:
  --scenario <id|name|all>   Scenario selector. Default: all
  --iterations <1-20>        Measured iterations. Default: 3
  --connection <value>       SQL Server connection string
  --output <path>            Optional Markdown result report

Environment:
  SQLPERFLAB_CONNECTION_STRING

Examples:
  dotnet run --project src/SqlPerformanceLab.Runner -- setup
  dotnet run --project src/SqlPerformanceLab.Runner -- list
  dotnet run --project src/SqlPerformanceLab.Runner -- run --scenario all --iterations 3
""");
}
