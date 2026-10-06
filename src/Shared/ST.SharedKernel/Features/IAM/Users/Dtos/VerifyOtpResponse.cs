

namespace ST.SharedKernel.Features.IAM.Users.Dtos;
public record VerifyOtpResponse(
    string Token,
    string RefreshToken,
    string UserId,
    string? SessionId,
    bool IsAuthenticated,
    DateTimeOffset ExpiresAt,
    AppUserResponse UserInfo,
    List<UserClaimsResponse>? UserClaims
    );
