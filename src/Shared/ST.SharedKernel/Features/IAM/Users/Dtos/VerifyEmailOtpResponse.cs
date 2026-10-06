using System;
using System.Collections.Generic;
using System.Text;

namespace ST.SharedKernel.Features.IAM.Users.Dtos;


public sealed record VerifyEmailOtpResponse(
    string Token,
    string RefreshToken,
    string UserId,
    string? SessionId,
    bool Success,
    DateTimeOffset ExpiresAt,
    AppUserResponse User,
    List<UserClaimsResponse> Claims
);
