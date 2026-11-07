using System.Net;
using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.Shared.Notifications.Contracts.Interfaces;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Infrastructure.Configuration;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Notifications.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class InviteAdminUser(UserManager<AppUser> users, ICurrentTenantProvider tenantProvider,
    IEmailService email, IOptions<EmailSettings> settings, ILogger<InviteAdminUser> logger)
    : IRequestHandler<InviteAdminUserCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(InviteAdminUserCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var user = await users.FindByIdAsync(command.UserId).ConfigureAwait(false);
            if (user is null || user.IsDeleted || user.TenantId != tenantProvider.TenantId || user.TenantId == Guid.Empty)
                return AppResponses.NotFound<bool>("User not found.");
            if (!user.IsActive || user.EmailConfirmed || string.IsNullOrWhiteSpace(user.Email))
                return AppResponses.Failure<bool>("Activate an account with an unverified email before sending an invitation.");
            if (string.IsNullOrWhiteSpace(settings.Value.Provider) || string.Equals(settings.Value.Provider, "NoOp", StringComparison.OrdinalIgnoreCase))
                return AppResponses.Failure<bool>("Account invitation delivery requires an email provider.");
            if (!Uri.TryCreate(settings.Value.ClientBaseUrl, UriKind.Absolute, out var baseUri)
                || (baseUri.Scheme != Uri.UriSchemeHttps && baseUri.Scheme != Uri.UriSchemeHttp)
                || !string.IsNullOrEmpty(baseUri.UserInfo) || !string.IsNullOrEmpty(baseUri.Query) || !string.IsNullOrEmpty(baseUri.Fragment))
                return AppResponses.Failure<bool>("Account invitation delivery is not configured.");

            var token = await users.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, AccountInvitation.Purpose(user)).ConfigureAwait(false);
            var link = new Uri(baseUri, $"/iam/accept-invitation?userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(token)}&tenantId={user.TenantId:D}");
            var result = await email.SendEmailAsync(new SendEmailRequest
            {
                To = user.Email,
                Subject = "Your account invitation",
                Body = $"<p>Hi {WebUtility.HtmlEncode(user.FirstName)},</p><p>Choose your password and confirm your email to complete account setup.</p><p><a href=\"{WebUtility.HtmlEncode(link.AbsoluteUri)}\">Accept invitation</a></p><p>If the link has expired, ask your administrator for another invitation.</p>"
            }, cancellationToken).ConfigureAwait(false);
            return result.IsSuccess
                ? AppResponses.Success("Account invitation sent.", true)
                : AppResponses.Failure<bool>("The invitation could not be delivered. Please try again.");
        }
        catch (Exception ex)
        {
            AccountInvitationLogDefinitions.LogFailed(logger, command.UserId, ex);
            throw;
        }
    }
}
