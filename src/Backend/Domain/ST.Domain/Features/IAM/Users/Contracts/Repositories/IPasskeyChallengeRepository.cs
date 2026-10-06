using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Contracts.Repositories;

namespace ST.Domain.Features.IAM.Users.Contracts.Repositories;

public interface IPasskeyChallengeRepository : IRepository<PasskeyChallenge>
{
    Task ReplaceAsync(PasskeyChallenge challenge, CancellationToken cancellationToken);
    Task<string?> ConsumeAsync(string key, DateTime utcNow, CancellationToken cancellationToken);
}
