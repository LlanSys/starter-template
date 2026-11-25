using Npgsql;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Globalization;

namespace ST.Api.Health;

internal sealed class PostgreSqlHealthCheck(IConfiguration configuration) : IHealthCheck
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
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 5;

            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return Convert.ToInt32(result, CultureInfo.InvariantCulture) == 1
                ? HealthCheckResult.Healthy("PostgreSQL is reachable.")
                : HealthCheckResult.Unhealthy("PostgreSQL probe returned an unexpected result.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL probe failed.", ex);
        }
    }
}
