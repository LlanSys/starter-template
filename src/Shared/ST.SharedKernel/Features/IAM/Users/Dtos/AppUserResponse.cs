namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public sealed record AppUserResponse(
    string Id,
    string Username,
    string FirstName,
    string LastName,
    string FullName,
    string? PhoneNumber,
    string Email,
    Uri? ProfilePictureUrl,
    bool IsActive,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    bool RequirePasswordChange,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    ICollection<string> Roles,
    Guid TenantId);
