namespace ST.Domain.Features.IAM.Users.Entities;

public sealed class SsoAuthorizationCode
{
    public required string Hash { get; init; }
    public required string UserId { get; init; }
    public required string SecurityStampHash { get; init; }
    public required Guid TenantId { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public string? ConnectionProvider { get; init; }
}
