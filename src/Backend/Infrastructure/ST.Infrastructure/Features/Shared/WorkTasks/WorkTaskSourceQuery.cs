using Microsoft.EntityFrameworkCore;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Enums;

namespace ST.Infrastructure.Features.Shared.WorkTasks;

internal sealed class WorkTaskSourceQuery(IControlPlaneUnitOfWork unit)
{
    public IQueryable<WorkTaskSource> ForKind(string kind, DateTimeOffset now, Guid? supportWorkspaceId)
    {
        var customers = unit.Tenants.FindAll().AsNoTracking()
            .Where(workspace => workspace.Id != Tenant.PlatformTenantId && !workspace.IsDemo);

        var retention = unit.KycRetentionCases.FindAll();

        switch (kind)
        {
            case "kyc-review":
                return customers.Where(workspace => workspace.KycApprovedAt == null
                        && !retention.Any(item => item.TenantId == workspace.Id && item.Outcome != "Open"))
                    .Select(workspace => new WorkTaskSource { Kind = kind, SubjectId = workspace.Id, CycleId = workspace.Id,
                        CycleAt = workspace.CreatedAt, TargetTenantId = workspace.Id, SubjectLabel = workspace.DisplayName });
            case "provisioning-failed":
                return customers.Where(workspace => workspace.Status == TenantStatus.ProvisioningFailed && workspace.KycApprovedAt != null)
                    .Select(workspace => new WorkTaskSource { Kind = kind, SubjectId = workspace.Id, CycleId = workspace.ProvisioningOperationId ?? Guid.Empty,
                        CycleAt = workspace.CreatedAt, TargetTenantId = workspace.Id, SubjectLabel = workspace.DisplayName });
            case "invitation-delivery":
                return unit.AdministratorInvitations.FindAll().AsNoTracking()
                    .Where(invitation => invitation.TenantId != Tenant.PlatformTenantId && !invitation.Tenant.IsDemo
                        && invitation.Tenant.Status == TenantStatus.Active && invitation.Tenant.KycApprovedAt != null
                        && invitation.State == AdministratorInvitationState.Pending
                        && ((invitation.DeliveredAt == null && invitation.Attempts > 0
                                && (invitation.LeaseUntil == null || invitation.LeaseUntil <= now)) || invitation.ExpiresAt <= now))
                    .Select(invitation => new WorkTaskSource { Kind = kind, SubjectId = invitation.Id, CycleId = invitation.Revision,
                        CycleAt = invitation.CreatedAt, TargetTenantId = invitation.TenantId, SubjectLabel = invitation.Tenant.DisplayName });
            case "hold-review":
                return from item in unit.KycRetentionCases.FindAll().AsNoTracking()
                       join workspace in customers on item.TenantId equals workspace.Id
                       where item.Held && item.HoldReviewDue <= now
                       select new WorkTaskSource { Kind = kind, SubjectId = item.TenantId, CycleId = item.TenantId,
                           CycleAt = item.HoldReviewDue!.Value, TargetTenantId = item.TenantId, SubjectLabel = workspace.DisplayName };
            case "support-approval":
                return from grant in unit.SupportGrants.FindAll().AsNoTracking()
                       join workspace in customers on grant.WorkspaceId equals workspace.Id
                       where grant.Status == "Pending" && workspace.Status == TenantStatus.Active
                           && (supportWorkspaceId == null || grant.WorkspaceId == supportWorkspaceId)
                       select new WorkTaskSource { Kind = kind, SubjectId = grant.Id, CycleId = grant.Id,
                           CycleAt = grant.CreatedAt, TargetTenantId = grant.WorkspaceId, SubjectLabel = workspace.DisplayName };
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }
}
