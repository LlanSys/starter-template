namespace ST.Api.Features.ControlPlane.Tenants.Dtos;

public record CompleteTenantProvisioningRequest(
    string DatabaseConnectionString,
    string ApplicationInsightsKey,
    Guid? OperationId = null,
    bool Succeeded = true,
    string? DatabaseProvider = null,
    string? ClientOrigin = null);
