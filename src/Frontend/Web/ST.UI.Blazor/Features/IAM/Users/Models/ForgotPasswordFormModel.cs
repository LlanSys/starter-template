using System.ComponentModel.DataAnnotations;

namespace ST.UI.Blazor.Features.IAM.Users.Models;

internal sealed class ForgotPasswordFormModel
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;
}
