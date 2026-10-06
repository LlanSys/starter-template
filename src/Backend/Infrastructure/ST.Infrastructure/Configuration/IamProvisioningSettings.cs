using System.ComponentModel.DataAnnotations;

namespace ST.Infrastructure.Configuration;

public sealed class IamProvisioningSettings
{
    public const string SectionName = "IamProvisioning";

    [Required]
    public string TemporaryPassword { get; init; } = string.Empty;

    [Required]
    public string SignInUrl { get; init; } = "https://localhost:7049/iam/sign-in";
}
