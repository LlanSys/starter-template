namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public sealed record AdminUserDeviceResponse(
    Guid Id,
    string AppUserId,
    string UserName,
    string Email,
    string DeviceName,
    string? IpAddress,
    bool IsTrusted,
    DateTimeOffset? LastUsedAt,
    DateTimeOffset? TrustedUntil);
