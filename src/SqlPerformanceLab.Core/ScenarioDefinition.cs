namespace SqlPerformanceLab.Core;

public sealed record ScenarioDefinition(
    string Id,
    string Name,
    string Description,
    string SetupSql,
    string BadSql,
    string GoodSql,
    string TeardownSql);
