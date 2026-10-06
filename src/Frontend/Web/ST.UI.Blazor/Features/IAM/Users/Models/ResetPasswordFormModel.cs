using System.ComponentModel.DataAnnotations;

namespace ST.UI.Blazor.Features.IAM.Users.Models;

internal sealed class ResetPasswordFormModel
{
    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(NewPassword), ErrorMessage = "Password confirmation must match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
