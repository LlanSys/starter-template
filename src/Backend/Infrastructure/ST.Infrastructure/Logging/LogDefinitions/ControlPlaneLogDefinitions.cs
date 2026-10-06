using Microsoft.Extensions.Logging;

namespace ST.Infrastructure.Logging;

internal static partial class ControlPlaneLogDefinitions
{
    [LoggerMessage(EventId = 2601, Level = LogLevel.Information, Message = "Created new deployment stamp {StampId} ({Name})")]
    public static partial void LogDeploymentStampCreated(ILogger logger, Guid stampId, string name);

    [LoggerMessage(EventId = 2602, Level = LogLevel.Information, Message = "Updated deployment stamp {StampId} ({Name})")]
    public static partial void LogDeploymentStampUpdated(ILogger logger, Guid stampId, string name);

    [LoggerMessage(EventId = 2603, Level = LogLevel.Information, Message = "Activated tenant {TenantId} ({Identifier})")]
    public static partial void LogTenantActivated(ILogger logger, Guid tenantId, string identifier);

    [LoggerMessage(EventId = 2604, Level = LogLevel.Information, Message = "Added/Activated module {ModuleKey} for tenant {TenantId}")]
    public static partial void LogTenantModuleAdded(ILogger logger, string moduleKey, Guid tenantId);

    [LoggerMessage(EventId = 2605, Level = LogLevel.Information, Message = "Created new tenant {TenantId} ({Identifier})")]
    public static partial void LogTenantCreated(ILogger logger, Guid tenantId, string identifier);

    [LoggerMessage(EventId = 2606, Level = LogLevel.Information, Message = "Deactivated module {ModuleKey} for tenant {TenantId}")]
    public static partial void LogTenantModuleRemoved(ILogger logger, string moduleKey, Guid tenantId);

    [LoggerMessage(EventId = 2607, Level = LogLevel.Information, Message = "Suspended tenant {TenantId} ({Identifier})")]
    public static partial void LogTenantSuspended(ILogger logger, Guid tenantId, string identifier);

    [LoggerMessage(EventId = 2608, Level = LogLevel.Information, Message = "Updated tenant {TenantId} ({Identifier})")]
    public static partial void LogTenantUpdated(ILogger logger, Guid tenantId, string identifier);

    [LoggerMessage(EventId = 2609, Level = LogLevel.Error, Message = "Stamp provisioning dispatch failed for tenant {TenantId}. Status set to ProvisioningFailed.")]
    public static partial void LogStampProvisioningFailed(ILogger logger, Guid tenantId, Exception exception);

    [LoggerMessage(EventId = 2610, Level = LogLevel.Information, Message = "Successfully dispatched isolated stamp provisioning workflow for tenant {TenantId} on stamp {StampId}")]
    public static partial void LogStampProvisioningDispatched(ILogger logger, Guid tenantId, string stampId);

    [LoggerMessage(EventId = 2611, Level = LogLevel.Warning, Message = "Tenant {TenantId} stamp changed. Revoking all active user sessions.")]
    public static partial void LogTenantStampChangedRevokingSessions(ILogger logger, Guid tenantId);

    [LoggerMessage(EventId = 2612, Level = LogLevel.Warning, Message = "Tenant {TenantId} module {ModuleKey} revoked. Revoking all active user sessions.")]
    public static partial void LogTenantModuleRevokedRevokingSessions(ILogger logger, Guid tenantId, string moduleKey);

    [LoggerMessage(EventId = 2613, Level = LogLevel.Warning, Message = "GitHub Action credentials are not configured. Provisioning skipped.")]
    public static partial void LogGitHubActionCredentialsNotConfigured(ILogger logger);

    [LoggerMessage(EventId = 2614, Level = LogLevel.Error, Message = "Failed to trigger GitHub Actions workflow for stamp provisioning. Status: {StatusCode}, Error: {Error}")]
    public static partial void LogGitHubActionTriggerFailed(ILogger logger, System.Net.HttpStatusCode statusCode, string error);

    [LoggerMessage(EventId = 2615, Level = LogLevel.Information, Message = "Successfully triggered GitHub Actions workflow for stamp {StampId}")]
    public static partial void LogGitHubActionTriggerSuccess(ILogger logger, string stampId);
}
