using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Infrastructure.Security;
using ST.Persistence.Features.IAM.DataContext;

namespace ST.Infrastructure.Features.IAM.Users.Contracts.Implementations.Services;

internal sealed class SsoCodeStore(
    IamDBContext iam, IIamUnitOfWork unitOfWork, ICurrentTenantProvider scope, UserManager<AppUser> users, TimeProvider timeProvider)
{
    public async Task<string> IssueAsync(AppUser user, string? connectionProvider, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var tenantId = scope.TenantId;
        if (user.TenantId != tenantId)
        {
            throw new InvalidOperationException("The sign-in account must belong to the current workspace.");
        }

        await iam.SsoAuthorizationCodes.Where(code => code.TenantId == tenantId && code.ExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

        var credential = SingleUseCredential.Create();
        iam.SsoAuthorizationCodes.Add(new SsoAuthorizationCode
        {
            Hash = SingleUseCredential.Hash(credential),
            TenantId = user.TenantId,
            UserId = user.Id,
            SecurityStampHash = SingleUseCredential.Hash(user.SecurityStamp ?? string.Empty),
            ExpiresAt = now.AddMinutes(2),
            ConnectionProvider = connectionProvider
        });
        await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
        return credential;
    }

    public async Task<AppUser?> ConsumeAsync(string credential, string? connectionProvider, CancellationToken cancellationToken)
    {
        if (credential is not { Length: 43 })
        {
            return null;
        }

        var hash = SingleUseCredential.Hash(credential);
        var tenantId = scope.TenantId;
        var now = timeProvider.GetUtcNow();
        var query = iam.SsoAuthorizationCodes.Where(code => code.Hash == hash && code.TenantId == tenantId
            && code.ExpiresAt > now && code.ConnectionProvider == connectionProvider);
        var code = await query.AsNoTracking().SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (code is null || await query.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false) != 1)
        {
            return null;
        }

        var user = await users.FindByIdAsync(code.UserId).ConfigureAwait(false);
        return user is not null && user.IsActive && !user.IsDeleted && user.EmailConfirmed && user.TenantId == tenantId
            && code.SecurityStampHash == SingleUseCredential.Hash(user.SecurityStamp ?? string.Empty) ? user : null;
    }
}
