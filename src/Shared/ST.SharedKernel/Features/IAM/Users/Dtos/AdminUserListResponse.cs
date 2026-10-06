namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public sealed record AdminUserListResponse(
    string Id,
    string UserName,
    string FullName,
    string Email,
    string? PhoneNumber,
    bool IsActive,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    bool RequirePasswordChange,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<string> Roles);
