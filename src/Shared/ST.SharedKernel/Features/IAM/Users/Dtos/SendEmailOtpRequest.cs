using ST.SharedKernel.Features.Shared.EmailTemplates.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ST.SharedKernel.Features.IAM.Users.Dtos;


public sealed record SendEmailOtpRequest
{
    [Required]
    public string UserId { get; init; } = string.Empty;
    public string Purpose { get; init; } = "Login";
}