using Microsoft.Data.SqlClient;

namespace SqlPerformanceLab.Runner;

internal static class SqlConnectionFactory
{
    public static string ResolveConnectionString(string? explicitConnection)
    {
        if (!string.IsNullOrWhiteSpace(explicitConnection))
            return explicitConnection;

        string? environment = Environment.GetEnvironmentVariable("SQLPERFLAB_CONNECTION_STRING");

        if (!string.IsNullOrWhiteSpace(environment))
            return environment;

        return "Server=localhost,1435;Database=master;User Id=sa;Password=SqlPerfLab_2026!;TrustServerCertificate=True;Encrypt=False";
    }

    public static string WithDatabase(string connectionString, string database)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = database
        };

        return builder.ConnectionString;
    }

    public static async Task<SqlConnection> OpenWithRetryAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        Exception? lastError = null;

        for (int attempt = 1; attempt <= 30; attempt++)
        {
            try
            {
                var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);
                return connection;
            }
            catch (Exception ex)
            {
                lastError = ex;

                if (attempt == 30)
                    break;

                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }

        throw new InvalidOperationException("Could not connect to SQL Server after 60 seconds.", lastError);
    }
}
