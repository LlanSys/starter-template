namespace ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;

public class CreateTenantRequest
{
    public required string Identifier { get; set; }
    public required string DisplayName { get; set; }
    public required string HostName { get; set; }
    public string? ContactEmail { get; set; }
    public int MaxUsers { get; set; }
    public required string SubscriptionTier { get; set; }

    public Guid DeploymentStampId { get; set; }

    public InitializeWorkspaceRequest? InitialAdministrator { get; set; }

    public string? DatabaseProvider { get; set; }
    public string? DatabaseConnectionString { get; set; }
}
