namespace ST.Domain.Features.IAM.Contracts;

public interface ITenantAccountLimitProvider
{
    Task<int?> GetLimitAsync(Guid tenantId, CancellationToken cancellationToken);
}
