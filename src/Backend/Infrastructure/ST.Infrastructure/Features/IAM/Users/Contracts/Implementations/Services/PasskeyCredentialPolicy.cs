using System.Security.Cryptography;
using System.Text;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Contracts.Common;
using Microsoft.EntityFrameworkCore;

namespace ST.Infrastructure.Features.IAM.Users.Contracts.Implementations.Services;

internal sealed class PasskeyCredentialPolicy(IIamUnitOfWork unitOfWork, ICurrentTenantProvider tenantProvider)
{
    internal bool CanUseAccount(AppUser user) => user.IsActive && !user.IsDeleted && user.EmailConfirmed
        && user.TenantId != Guid.Empty && user.TenantId == tenantProvider.TenantId;

    internal async Task<bool> IsUniqueAsync(AppUser user, byte[] credentialId, CancellationToken cancellationToken)
        => CanUseAccount(user) && credentialId.Length > 0 && !await unitOfWork.Fido2CredentialRepository
            .FindByCondition(credential => credential.CredentialId == credentialId)
            .IgnoreQueryFilters().AnyAsync(cancellationToken).ConfigureAwait(false);

    internal async Task<bool> IsOwnerAsync(AppUser user, byte[] credentialId, byte[]? userHandle, CancellationToken cancellationToken)
    {
        if (!CanUseAccount(user) || credentialId.Length == 0 || userHandle is null || userHandle.Length == 0
            || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(user.Id), userHandle)) return false;
        return await unitOfWork.Fido2CredentialRepository.AnyAsync(credential => credential.UserId == user.Id
            && credential.TenantId == user.TenantId && !credential.IsDeleted
            && credential.CredentialId == credentialId && credential.UserHandle == userHandle, cancellationToken).ConfigureAwait(false);
    }
}
