namespace ST.SharedKernel.Features.ControlPlane.Stamps.Dtos;

public class UpdateDeploymentStampRequest
{
    public required string Name { get; set; }
    public required string TargetResourceGroup { get; set; }
    public required string IsolationTier { get; set; }

    public string? KeyVaultUri { get; set; }

    public string? DatabaseProvider { get; set; }
    public string? DatabaseConnectionString { get; set; }
}
