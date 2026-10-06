namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public sealed record CurrentUserResponse(
    string AppUserId,
    Guid TenantId,
    string UserName,
    string Email,
    string FirstName,
    string LastName,
    string PhoneNumber,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    bool? IsAuthenticated,
    DateTimeOffset? LastLoginAt,
    List<string> Roles,
    List<string>? Permissions = null,
    string? SessionId = null,
    bool MfaEnrollmentRequired = false,
    Uri? ProfilePictureUrl = null);
