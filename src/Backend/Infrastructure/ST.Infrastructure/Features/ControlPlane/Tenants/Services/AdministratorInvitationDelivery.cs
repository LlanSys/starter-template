using Microsoft.Extensions.Logging;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ST.Application.Features.Shared.Notifications.Contracts.Interfaces;
using ST.Application.Features.ControlPlane.Tenants.Contracts;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Infrastructure.Configuration;
using ST.Infrastructure.Security;
using ST.SharedKernel.Features.Shared.Notifications.Dtos;

namespace ST.Infrastructure.Features.ControlPlane.Tenants.Services;

internal sealed partial class AdministratorInvitationDelivery(
    IControlPlaneUnitOfWork plane, IEmailService emailService, IOptions<EmailSettings> emailSettings,
    IOptions<AdministratorInvitationSettings> settings, ITenantWorkspaceProvisioner provisioner,
    TimeProvider timeProvider, ILogger<AdministratorInvitationDelivery> logger)
{
    public async Task DeliverAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var items = await plane.AdministratorInvitations.FindAll()
            .AsTracking()
            .Include(item => item.Tenant)
            .Where(item => item.State == AdministratorInvitationState.Pending
                && item.DeliveredAt == null && item.NextAttemptAt <= now
                && (item.LeaseUntil == null || item.LeaseUntil <= now)
                && item.Tenant.KycApprovedAt != null && item.Tenant.Status == TenantStatus.Active
                && !item.Tenant.IsDemo)
            .OrderBy(item => item.NextAttemptAt)
            .Take(10)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var invitation in items)
        {
            var credential = SingleUseCredential.Create();
            now = timeProvider.GetUtcNow();
            if (!invitation.TryIssue(SingleUseCredential.Hash(credential), now, TimeSpan.FromHours(settings.Value.ValidityHours)))
            {
                continue;
            }

            // Save the lease before making a call that cannot be rolled back.
            await plane.CompleteAsync(cancellationToken).ConfigureAwait(false);

            var eligible = await plane.Tenants.AnyAsync(tenant => tenant.Id == invitation.TenantId
                && tenant.Status == TenantStatus.Active && tenant.KycApprovedAt != null, cancellationToken).ConfigureAwait(false);
            if (eligible && invitation.Tenant.WorkspaceVersion == 0)
            {
                var setup = await provisioner.InitializeAsync(invitation.TenantId, null, invitation.CreatedBy, cancellationToken)
                    .ConfigureAwait(false);
                eligible = setup.IsSuccess;
            }

            var sent = eligible && await SendAsync(invitation.Id, invitation.Tenant.Identifier, invitation.Email,
                invitation.FirstName, invitation.Tenant.DisplayName, credential, cancellationToken).ConfigureAwait(false);
            invitation.ReportDelivery(sent, timeProvider.GetUtcNow());
            await plane.CompleteAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<bool> SendAsync(Guid id, string workspace, string recipient, string name, string organization,
        string credential, CancellationToken cancellationToken)
    {
        var configuration = emailSettings.Value;
        if (string.IsNullOrWhiteSpace(configuration.Provider) || string.Equals(configuration.Provider, "NoOp", StringComparison.OrdinalIgnoreCase)
            || !Uri.TryCreate(configuration.ClientBaseUrl, UriKind.Absolute, out var origin)
            || (origin.Scheme != Uri.UriSchemeHttps && !(origin.Scheme == Uri.UriSchemeHttp && origin.IsLoopback))
            || !string.IsNullOrEmpty(origin.UserInfo) || !string.IsNullOrEmpty(origin.Query) || !string.IsNullOrEmpty(origin.Fragment))
        {
            return false;
        }

        var link = new Uri(origin, $"/iam/administrator-invitation?workspace={Uri.EscapeDataString(workspace)}&invitation={id:D}&token={credential}");
        try
        {
            var result = await emailService.SendEmailAsync(new SendEmailRequest
            {
                To = recipient,
                Subject = "Your workspace administrator invitation",
                Body = $"<p>Hello {WebUtility.HtmlEncode(name)},</p><p>You have been invited to administer {WebUtility.HtmlEncode(organization)}. Open the invitation to review it and finish setting up your account.</p><p><a href=\"{WebUtility.HtmlEncode(link.AbsoluteUri)}\">Open administrator invitation</a></p>"
            }, cancellationToken).ConfigureAwait(false);
            return result.IsSuccess;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogDeliveryFailure(logger, id, exception.GetType().Name);
            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Administrator invitation {InvitationId} delivery failed ({FailureType}).")]
    private static partial void LogDeliveryFailure(ILogger logger, Guid invitationId, string failureType);
}
