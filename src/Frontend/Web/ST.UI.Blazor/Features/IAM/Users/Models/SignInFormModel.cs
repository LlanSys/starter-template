using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ST.UI.Blazor.Features.IAM.Users.Models;

[SuppressMessage(
    "Maintainability",
    "CA1515:Consider making public types internal",
    Justification = "Blazor static SSR form mapping binds this model during published/container form posts.")]
public sealed class SignInFormModel
{
    [Required(ErrorMessage = "Username or email is required.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
