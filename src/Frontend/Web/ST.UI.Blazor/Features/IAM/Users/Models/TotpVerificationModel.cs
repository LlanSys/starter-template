using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ST.UI.Blazor.Features.IAM.Users.Models;

[SuppressMessage(
    "Maintainability",
    "CA1515:Consider making public types internal",
    Justification = "Blazor static SSR form mapping binds this model during published/container form posts.")]
public sealed class TotpVerificationModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the 6-digit authenticator code.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "The authenticator code must be 6 digits.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "The authenticator code can only contain digits.")]
    public string Code { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public bool RememberDevice { get; set; }

    public string DeviceFingerprint { get; set; } = "blazor-server";
}
