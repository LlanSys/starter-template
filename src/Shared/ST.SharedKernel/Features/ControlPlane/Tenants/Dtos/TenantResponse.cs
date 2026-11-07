using System;

namespace ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;

public class TenantResponse
{
    public Guid Id { get; set; }
    public required string Identifier { get; set; }
    public required string DisplayName { get; set; }
    public required string HostName { get; set; }
    public string? ContactEmail { get; set; }
    public int MaxUsers { get; set; }
    public required string SubscriptionTier { get; set; }
    public required string Status { get; set; }

    public Guid DeploymentStampId { get; set; }

    public string? DatabaseProvider { get; set; }
    public string? DatabaseConnectionString { get; set; }

    public IReadOnlyList<string> EnabledModules { get; set; } = [];

    public DateTimeOffset? KycApprovedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
