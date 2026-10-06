using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ST.Api.Health;

internal sealed class SqlServerHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString) ||
            connectionString.Contains("REPLACE_", StringComparison.OrdinalIgnoreCase))
        {
            return HealthCheckResult.Unhealthy("DefaultConnection is not configured.");
        }

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 5;

            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture) == 1
                ? HealthCheckResult.Healthy("SQL Server is reachable.")
                : HealthCheckResult.Unhealthy("SQL Server probe returned an unexpected result.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQL Server probe failed.", ex);
        }
    }
}
