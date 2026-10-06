using System.ComponentModel.DataAnnotations;

namespace ST.UI.Blazor.Features.IAM.Users.Models;

public sealed class ChangePasswordFormModel
{
    [Required(ErrorMessage = "Current password is required.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [MinLength(8, ErrorMessage = "New password must be at least 8 characters.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your new password.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
