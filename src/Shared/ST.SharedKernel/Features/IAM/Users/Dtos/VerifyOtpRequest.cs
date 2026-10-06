using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public sealed record VerifyOtpRequest
{
    [Required]
    public string UserId { get; init; } = string.Empty;

    [Required(ErrorMessage = "Verification code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be exactly 6 digits")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must contain only digits")]
    public string Code { get; init; } = string.Empty;

    public bool RememberMe { get; init; }

    public bool RememberDevice { get; init; }

    public string? DeviceFingerprint { get; init; }
}
