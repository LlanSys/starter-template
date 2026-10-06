using System.ComponentModel.DataAnnotations;

namespace ST.Infrastructure.Configuration;

public sealed class OrgSettings
{
    public const string SectionName = "Tenant";

    [Required]
    public Guid DefaultTenantId { get; init; }

    [Required]
    public string HeaderName { get; init; } = "X-Tenant-Id";
}
