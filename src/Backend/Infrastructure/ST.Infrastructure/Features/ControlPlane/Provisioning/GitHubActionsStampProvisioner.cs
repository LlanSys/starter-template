using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using ST.Application.Features.ControlPlane.Tenants.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using ST.Infrastructure.Logging;

namespace ST.Infrastructure.Features.ControlPlane.Provisioning;

public class GitHubActionsStampProvisioner(HttpClient httpClient, IConfiguration configuration, ILogger<GitHubActionsStampProvisioner> logger) : IStampProvisioner
{
    public async Task ProvisionIsolatedStampAsync(string tenantId, string stampId, string resourceGroup, string databaseProvider, Guid operationId, CancellationToken cancellationToken = default)
    {
        if (!bool.TryParse(configuration["ControlPlane:Provisioning:Enabled"],out var enabled) || !enabled) throw new InvalidOperationException("Isolated provisioning is disabled for this host.");
        var githubToken = configuration["ControlPlane:GitHub:Pat"];
        var repoOwner = configuration["ControlPlane:GitHub:RepoOwner"];
        var repoName = configuration["ControlPlane:GitHub:RepoName"];

        if (string.IsNullOrEmpty(githubToken) || string.IsNullOrEmpty(repoOwner) || string.IsNullOrEmpty(repoName))
        {
            ControlPlaneLogDefinitions.LogGitHubActionCredentialsNotConfigured(logger);
            throw new InvalidOperationException("Isolated stamp provisioning is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.github.com/repos/{repoOwner}/{repoName}/actions/workflows/provision-isolated-stamp.yml/dispatches");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("StarterTemplate", "1.0"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

        var payload = new
        {
            @ref = "main",
            inputs = new
            {
                tenant_id = tenantId,
                operation_id = operationId.ToString(),
                stamp_id = stampId,
                resource_group = resourceGroup,
                database_provider = databaseProvider
            }
        };

        request.Content = JsonContent.Create(payload);

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            ControlPlaneLogDefinitions.LogGitHubActionTriggerFailed(logger, response.StatusCode, error);
            throw new InvalidOperationException(
                $"Stamp provisioning workflow dispatch failed with HTTP {(int)response.StatusCode}. " +
                "Check GitHub Actions credentials and workflow configuration.");
        }

        ControlPlaneLogDefinitions.LogGitHubActionTriggerSuccess(logger, stampId);
    }
}
