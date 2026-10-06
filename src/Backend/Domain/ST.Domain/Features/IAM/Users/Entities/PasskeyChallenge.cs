using ST.Domain.Shared.Entities;

namespace ST.Domain.Features.IAM.Users.Entities;

public sealed class PasskeyChallenge : BaseEntity
{
    public required string Key { get; init; }
    public required string OptionsJson { get; init; }
    public DateTime ExpiresAtUtc { get; init; }
}
