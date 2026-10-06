
using System.Security.Claims;

namespace ST.SharedKernel.Features.IAM.Users.Dtos;
public record RefreshTokenResponse(
    string Token,
    string RefreshToken,
    string UserId,
    string? SessionId,
    DateTimeOffset TokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt,
    AppUserResponse UserInfo,
    List<Claim> UserClaims
);
