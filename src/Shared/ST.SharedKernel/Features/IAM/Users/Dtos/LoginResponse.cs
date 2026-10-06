
namespace ST.SharedKernel.Features.IAM.Users.Dtos;
public record LoginResponse(
    string UserId,
    string FirstName,
    string LastName,
    string Email,
    bool Requires2FA,
    bool RequiresEmailConfirmation,
    bool IsAuthenticated,
    string Token,
    string RefreshToken,
    string? SessionId,
    DateTimeOffset TokenExpiresAt,
    AppUserResponse? UserInfo,
    List<ClaimResponse>? UserClaims,
    bool MfaEnrollmentRequired = false

    );
