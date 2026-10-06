using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using ST.Api.Configuration;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ST.Api.Health;

internal sealed class KeyVaultHealthCheck(
    IConfiguration configuration,
    IOptions<HealthCheckSettings> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.KeyVault)
        {
            return HealthCheckResult.Healthy("Key Vault probe is disabled.");
        }

        var keyVaultUri = configuration["KeyVault:Uri"];
        if (string.IsNullOrWhiteSpace(keyVaultUri))
        {
            return HealthCheckResult.Unhealthy("KeyVault:Uri is not configured.");
        }

        if (string.IsNullOrWhiteSpace(settings.KeyVaultProbeSecretName))
        {
            return HealthCheckResult.Unhealthy("HealthChecks:KeyVaultProbeSecretName is required when Key Vault health probing is enabled.");
        }

        try
        {
            var client = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
            await client.GetSecretAsync(settings.KeyVaultProbeSecretName, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return HealthCheckResult.Healthy("Key Vault probe secret is readable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Key Vault probe failed.", ex);
        }
    }
}
