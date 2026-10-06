using ST.Domain.Shared.Contracts.Repositories;
using ST.Domain.Features.IAM.Users.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Domain.Features.IAM.Users.Contracts.Repositories;

public interface ITempTotpSecretRepository : IRepository<TempTotpSecret>
{
    Task<TempTotpSecret?> GetValidTempSecretByUserIdAsync(string userId);
    Task<bool> DeleteExpiredSecretsAsync(CancellationToken cancellationToken);
    Task<bool> DeleteUserTempSecretsAsync(string userId, CancellationToken cancellationToken);
}
