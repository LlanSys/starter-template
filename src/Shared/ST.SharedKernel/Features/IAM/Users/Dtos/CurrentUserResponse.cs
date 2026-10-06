namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public record CurrentUserResponse(
    string AppUserId,
    Guid StaffId,
    Guid CustomerId,
    string IdNumber,
    string UserName,
    string Email,
    string FirstName,
    string LastName,
    string PhoneNumber,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    string Gender,
    bool? IsAuthenticated,
    DateTimeOffset? LastLoginAt,
    List<string> Roles,
    List<string>? Permissions = null,
    string? SessionId = null,
    bool MfaEnrollmentRequired = false,
    Uri? ProfilePictureUrl = null);
