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

internal sealed class ActivateAdminUser(UserManager<AppUser> userManager, IIamUnitOfWork unitOfWork,
    ICurrentTenantProvider tenantProvider, ILogger<ActivateAdminUser> logger, TimeProvider timeProvider)
    : IRequestHandler<ActivateAdminUserCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(ActivateAdminUserCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var user = await userManager.FindByIdAsync(command.UserId).ConfigureAwait(false);
            if (user is null || user.IsDeleted || tenantProvider.TenantId == Guid.Empty || user.TenantId != tenantProvider.TenantId)
                return AppResponses.NotFound<bool>("User not found.");
            if (user.IsActive)
                return AppResponses.Success("User is already active.", true);

            return await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var now = timeProvider.GetUtcNow();
                user.GrantAccess(now, command.ActivatedBy);
                // Identity saves clear aggregate events, so stage the event for the UoW's pre-commit dispatch.
                var events = user.DomainEvents.ToArray();
                user.ClearDomainEvents();
                var result = await userManager.UpdateSecurityStampAsync(user).ConfigureAwait(false);
                if (!result.Succeeded)
                    throw new ActivationRejectedException();
                await AccountAccessCleanup.StageAsync(unitOfWork, user.Id, command.ActivatedBy,
                    "Account access reactivated", now, cancellationToken).ConfigureAwait(false);
                await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
                foreach (var domainEvent in events) user.RaiseDomainEvent(domainEvent);
                return AppResponses.Success("User activated. Existing password and email verification are preserved.", true);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (ActivationRejectedException ex)
        {
            AccountAccessLogDefinitions.LogActivationFailed(logger, command.UserId, ex);
            return AppResponses.Failure<bool>("The account could not be activated. Please try again.");
        }
        catch (Exception ex)
        {
            AccountAccessLogDefinitions.LogActivationFailed(logger, command.UserId, ex);
            throw;
        }
    }

    private sealed class ActivationRejectedException : Exception { }
}
