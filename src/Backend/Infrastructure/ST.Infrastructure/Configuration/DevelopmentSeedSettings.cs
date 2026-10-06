using System.ComponentModel.DataAnnotations;

namespace ST.Infrastructure.Configuration;

public sealed class DevelopmentSeedSettings
{
    public const string SectionName = "DevelopmentSeed";

    public bool Enabled { get; init; }

    [Required]
    public string AdminUserName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    public string AdminEmail { get; init; } = string.Empty;

    [Required]
    public string AdminPassword { get; init; } = string.Empty;

    public bool ResetExistingAdminPassword { get; init; }

    public Guid TenantId { get; init; } = new("0194f700-0000-7000-8000-000000000001");
}
