using Xunit;
using SqlPerformanceLab.Core;

namespace SqlPerformanceLab.Tests;

public sealed class SqlBatchSplitterTests
{
    [Fact]
    public void Split_GoSeparators_ReturnsBatches()
    {
        const string sql = """
            SELECT 1;
            GO
            SELECT 2;
            go
            SELECT 3;
            """;

        IReadOnlyList<string> result = SqlBatchSplitter.Split(sql);

        Assert.Equal(3, result.Count);
        Assert.Contains("SELECT 1", result[0]);
        Assert.Contains("SELECT 2", result[1]);
        Assert.Contains("SELECT 3", result[2]);
    }

    [Fact]
    public void Split_EmptyBatches_AreIgnored()
    {
        const string sql = """
            GO
            SELECT 1;
            GO
            GO
            """;

        IReadOnlyList<string> result = SqlBatchSplitter.Split(sql);
        Assert.Single(result);
    }

    [Fact]
    public void Split_NoGo_ReturnsOneBatch()
    {
        IReadOnlyList<string> result = SqlBatchSplitter.Split("SELECT 1;");
        Assert.Single(result);
    }
}
