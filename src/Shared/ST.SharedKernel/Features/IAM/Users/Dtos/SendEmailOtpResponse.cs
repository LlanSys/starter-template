using System;
using System.Collections.Generic;
using System.Text;

namespace ST.SharedKernel.Features.IAM.Users.Dtos;


public sealed record SendEmailOtpResponse(
    string UserId,
    DateTimeOffset ExpiresAt,
    int CooldownSeconds
);