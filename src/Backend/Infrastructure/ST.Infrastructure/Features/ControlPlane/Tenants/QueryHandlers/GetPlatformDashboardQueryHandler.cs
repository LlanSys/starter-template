using MediatR;
using Microsoft.EntityFrameworkCore;
using ST.Application.Features.ControlPlane.Tenants.Queries;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;

namespace ST.Infrastructure.Features.ControlPlane.Tenants.QueryHandlers;

public sealed class GetPlatformDashboardQueryHandler(IControlPlaneUnitOfWork plane)
    : IRequestHandler<GetPlatformDashboardQuery, AppResponse<PlatformDashboardResponse>>
{
    public async Task<AppResponse<PlatformDashboardResponse>> Handle(
        GetPlatformDashboardQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Group in the database; never transport workspace records or private configuration to the dashboard.
        var totals = await plane.Tenants.FindAll()
            .AsNoTracking()
            .Where(tenant => tenant.Id != Tenant.PlatformTenantId && !tenant.IsDemo)
            .GroupBy(tenant => 1)
            .Select(group => new
            {
                Total = group.Count(),
                Active = group.Count(tenant => tenant.Status == TenantStatus.Active),
                Suspended = group.Count(tenant => tenant.Status == TenantStatus.Suspended),
                Kyc = group.Count(tenant => tenant.KycApprovedAt == null),
                Pending = group.Count(tenant => tenant.Status == TenantStatus.PendingProvisioning),
                Provisioning = group.Count(tenant => tenant.Status == TenantStatus.Provisioning),
                Failed = group.Count(tenant => tenant.Status == TenantStatus.ProvisioningFailed)
            })
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        int? invitationsPending = null;
        int? deliveryRetries = null;
        if (request.IncludeInvitations)
        {
            var invitations = await plane.AdministratorInvitations.FindAll()
                .AsNoTracking()
                .Where(invitation => invitation.TenantId != Tenant.PlatformTenantId && !invitation.Tenant.IsDemo
                    && (invitation.State == AdministratorInvitationState.Pending
                        || invitation.State == AdministratorInvitationState.Claimed))
                .GroupBy(invitation => 1)
                .Select(group => new
                {
                    Pending = group.Count(),
                    Retries = group.Count(invitation => invitation.State == AdministratorInvitationState.Pending
                        && invitation.Attempts > 0 && invitation.DeliveredAt == null && invitation.LeaseUntil == null)
                })
                .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            invitationsPending = invitations?.Pending ?? 0;
            deliveryRetries = invitations?.Retries ?? 0;
        }

        return AppResponses.Success(new PlatformDashboardResponse(
            totals?.Total ?? 0,
            totals?.Active ?? 0,
            totals?.Suspended ?? 0,
            totals?.Kyc ?? 0,
            totals?.Pending ?? 0,
            totals?.Provisioning ?? 0,
            totals?.Failed ?? 0,
            invitationsPending,
            deliveryRetries));
    }
}
