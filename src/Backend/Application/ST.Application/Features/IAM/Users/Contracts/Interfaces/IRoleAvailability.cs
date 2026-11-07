namespace ST.Application.Features.IAM.Users.Contracts.Interfaces;
public interface IRoleAvailability
{
    Task<IReadOnlySet<string>> AvailableIdsAsync(CancellationToken cancellationToken = default);
}
