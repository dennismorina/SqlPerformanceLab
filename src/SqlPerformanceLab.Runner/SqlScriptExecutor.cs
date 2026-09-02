using Microsoft.Data.SqlClient;
using SqlPerformanceLab.Core;

namespace SqlPerformanceLab.Runner;

internal static class SqlScriptExecutor
{
    public static async Task ExecuteScriptAsync(
        string connectionString,
        string script,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection =
            await SqlConnectionFactory.OpenWithRetryAsync(connectionString, cancellationToken);

        foreach (string batch in SqlBatchSplitter.Split(script))
        {
            await using var command = new SqlCommand(batch, connection)
            {
                CommandTimeout = 180
            };

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
