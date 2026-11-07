using MediatR;
using Microsoft.AspNetCore.Identity;
using ST.Application.Features.ControlPlane.Tenants.Contracts;
using ST.Application.Features.IAM.Users.Commands;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Infrastructure.Features.IAM.Users.Contracts.Implementations.Services;
using ST.SharedKernel.Dtos.Common;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class ProcessSsoLogin(
    UserManager<AppUser> users, ICurrentTenantProvider tenantScope, IAdministratorInvitationService invitations, SsoCodeStore codes)
    : IRequestHandler<ProcessSsoLoginCommand, AppResponse<string>>
{
    public async Task<AppResponse<string>> Handle(ProcessSsoLoginCommand command, CancellationToken cancellationToken)
    {
        if (command.Provider is not ("EntraId" or "Google"))
        {
            return AppResponses.Failure<string>("External sign-in is unavailable.");
        }

        AppUser? user;
        if (command.InvitationId is Guid invitationId)
        {
            if (string.IsNullOrEmpty(command.InvitationToken) || string.IsNullOrEmpty(command.VerifiedEmail))
            {
                return AppResponses.Failure<string>("The external account could not prove the invited email address.");
            }

            var accepted = await invitations.AcceptExternalAsync(invitationId, command.InvitationToken,
                command.Provider, command.ProviderKey, command.VerifiedEmail, cancellationToken).ConfigureAwait(false);
            if (!accepted.IsSuccess)
            {
                return AppResponses.Failure<string>(accepted.Message ?? "The invitation is unavailable.");
            }
            user = accepted.Data;
        }
        else
        {
            user = await users.FindByLoginAsync(command.Provider, command.ProviderKey).ConfigureAwait(false);
        }

        if (user is null || user.TenantId != tenantScope.TenantId || !user.IsActive || user.IsDeleted || !user.EmailConfirmed
            || await users.IsLockedOutAsync(user).ConfigureAwait(false))
        {
            return AppResponses.Failure<string>("Use your established sign-in method, or connect this provider in account security.");
        }

        return AppResponses.Success(await codes.IssueAsync(user, null, cancellationToken).ConfigureAwait(false));
    }
}
