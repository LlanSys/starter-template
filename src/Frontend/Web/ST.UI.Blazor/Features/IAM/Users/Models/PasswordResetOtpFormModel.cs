using System.ComponentModel.DataAnnotations;

namespace ST.UI.Blazor.Features.IAM.Users.Models;

internal sealed class PasswordResetOtpFormModel
{
    [Required]
    [RegularExpression("^[0-9]{6}$", ErrorMessage = "Enter the 6-digit code from your email.")]
    public string Code { get; set; } = string.Empty;
}
