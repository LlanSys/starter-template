using ST.SharedKernel.Features.Shared.EmailTemplates.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ST.SharedKernel.Features.IAM.Users.Dtos;


public sealed record VerifyEmailOtpRequest
{
    [Required]
    public string UserId { get; init; } = string.Empty;

    [Required(ErrorMessage = "Code is required")]
    [StringLength(6, MinimumLength = 6)]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be 6 digits")]
    public string Code { get; init; } = string.Empty;

    public string Purpose { get; init; } = "Login";
    public bool RememberMe { get; init; }
    public bool RememberDevice { get; init; }
    public string? DeviceFingerprint { get; init; }
}
