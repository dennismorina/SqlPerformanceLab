using Xunit;
using SqlPerformanceLab.Core;

namespace SqlPerformanceLab.Tests;

public sealed class ScenarioFileParserTests
{
    [Fact]
    public void Parse_ValidScenario_ReturnsAllSections()
    {
        const string content = """
            -- @scenario Example
            -- @description Demonstrates a query pattern.
            -- @setup
            SELECT 1;
            -- @bad
            SELECT 2;
            -- @good
            SELECT 3;
            -- @teardown
            SELECT 4;
            """;

        ScenarioDefinition result = ScenarioFileParser.Parse("01-example", content);

        Assert.Equal("01-example", result.Id);
        Assert.Equal("Example", result.Name);
        Assert.Equal("Demonstrates a query pattern.", result.Description);
        Assert.Contains("SELECT 1", result.SetupSql);
        Assert.Contains("SELECT 2", result.BadSql);
        Assert.Contains("SELECT 3", result.GoodSql);
        Assert.Contains("SELECT 4", result.TeardownSql);
    }

    [Fact]
    public void Parse_MissingBadSection_Throws()
    {
        const string content = """
            -- @scenario Example
            -- @description Description
            -- @good
            SELECT 1;
            """;

        Assert.Throws<FormatException>(() => ScenarioFileParser.Parse("example", content));
    }

    [Fact]
    public void Parse_MissingScenarioName_Throws()
    {
        const string content = """
            -- @description Description
            -- @bad
            SELECT 1;
            -- @good
            SELECT 2;
            """;

        Assert.Throws<FormatException>(() => ScenarioFileParser.Parse("example", content));
    }
}
