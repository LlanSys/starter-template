using System;

namespace ST.SharedKernel.Features.ControlPlane.Stamps.Dtos;

public class DeploymentStampResponse
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string TargetResourceGroup { get; set; }
    public required string IsolationTier { get; set; }
    public string? KeyVaultUri { get; set; }

    public string? DatabaseProvider { get; set; }
    public string? DatabaseConnectionString { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
