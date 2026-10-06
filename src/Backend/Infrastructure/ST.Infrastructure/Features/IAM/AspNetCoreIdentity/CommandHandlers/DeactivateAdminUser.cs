using ST.Application.Features.IAM.Users.Commands;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class DeactivateAdminUser(UserManager<AppUser> userManager, IIamUnitOfWork unitOfWork,
    ICurrentTenantProvider tenantProvider, ILogger<DeactivateAdminUser> logger)
    : IRequestHandler<DeactivateAdminUserCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(DeactivateAdminUserCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var user = await userManager.FindByIdAsync(command.UserId).ConfigureAwait(false);
            if (user is null || user.IsDeleted || tenantProvider.TenantId == Guid.Empty || user.TenantId != tenantProvider.TenantId)
                return AppResponses.NotFound<bool>("User not found.");
            var reason = string.IsNullOrWhiteSpace(command.Request.Reason)
                ? "Deactivated by administrator" : command.Request.Reason.Trim();

            return await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                if (user.IsActive) user.RevokeAccess(command.DeactivatedBy, reason);
                var events = user.DomainEvents.ToArray();
                user.ClearDomainEvents();
                var result = await userManager.UpdateSecurityStampAsync(user).ConfigureAwait(false);
                if (!result.Succeeded) throw new DeactivationRejectedException();
                // Also clean stale access on an already-inactive account, without rewriting its lifecycle audit.
                await AccountAccessCleanup.StageAsync(unitOfWork, user.Id, command.DeactivatedBy,
                    reason, cancellationToken).ConfigureAwait(false);
                await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
                foreach (var domainEvent in events) user.RaiseDomainEvent(domainEvent);
                return AppResponses.Success("User deactivated. Sessions and refresh tokens have been terminated.", true);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (DeactivationRejectedException ex)
        {
            ServiceLogDefinitions.LogDeactivateUserError(logger, command.UserId, ex);
            return AppResponses.Failure<bool>("The account could not be deactivated. Please try again.");
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogDeactivateUserError(logger, command.UserId, ex);
            throw;
        }
    }

    private sealed class DeactivationRejectedException : Exception { }
}
