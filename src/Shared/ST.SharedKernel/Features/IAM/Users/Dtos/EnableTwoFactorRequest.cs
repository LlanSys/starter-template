using System;
using System.Collections.Generic;
using System.Text;

namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public record EnableTwoFactorRequest
{
    public string VerificationCode { get; init; } = string.Empty;
}
