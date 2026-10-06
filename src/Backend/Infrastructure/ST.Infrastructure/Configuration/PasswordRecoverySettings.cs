using System.ComponentModel.DataAnnotations;

namespace ST.Infrastructure.Configuration;

public sealed class PasswordRecoverySettings
{
    public const string SectionName = "PasswordRecovery";

    public PasswordRecoveryMode Mode { get; init; } = PasswordRecoveryMode.EmailOtp;

    [Required]
    public string ResetPath { get; init; } = "/iam/reset-password";
}
