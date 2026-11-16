using MediatR;
using Microsoft.EntityFrameworkCore;
using ST.Application.Features.Shared.Dashboard;
using ST.Application.Features.Shared.WorkTasks;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane;
using ST.SharedKernel.Features.Shared.Dashboard;

namespace ST.Infrastructure.Features.Shared.Dashboard;

public sealed class GetDashboardAttentionQueryHandler(IControlPlaneUnitOfWork plane, ICurrentActorProvider actor,
    IWorkTaskService tasks, TimeProvider clock)
    : IRequestHandler<GetDashboardAttentionQuery, AppResponse<IReadOnlyList<DashboardAttentionItemResponse>>>
{
    public async Task<AppResponse<IReadOnlyList<DashboardAttentionItemResponse>>> Handle(GetDashboardAttentionQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Access);
        var items = new List<DashboardAttentionItemResponse>();
        var categories = new[]
        {
            ("kyc-review", request.Access.Kyc), ("provisioning-failed", request.Access.Provisioning),
            ("invitation-delivery", request.Access.Invitations), ("hold-review", request.Access.LegalHolds),
            ("support-approval", request.Access.SupportApprovals)
        };

        foreach (var (kind, allowed) in categories)
        {
            if (!allowed) continue;
            var definition = DashboardTaskCatalog.Definitions[kind];
            var counts = await tasks.CountsAsync(kind, cancellationToken).ConfigureAwait(false);
            Add(kind, definition.Title, counts.Unassigned, $"/tasks?kind={kind}&queue=unassigned", definition.RequiredPermissions,
                DashboardAttentionKind.TeamAction, definition.Reason, "Open team queue");

            Add($"assigned-{kind}", definition.Title, counts.Mine, $"/tasks?kind={kind}&queue=mine", definition.RequiredPermissions,
                DashboardAttentionKind.AssignedAction, "Assigned to you. " + definition.Reason, "Open My Tasks");
        }

        if (request.Access.ApprovedSupport)
        {
            var now = clock.GetUtcNow();
            var actorId = actor.ActorId;
            var count = await plane.SupportGrants.FindAll().AsNoTracking()
                .CountAsync(grant => grant.VendorId == actorId && grant.Status == "Approved" && grant.ExpiresAt > now, cancellationToken)
                .ConfigureAwait(false);

            Add("support-ready", "Approved customer support access is ready", count, "/control-panel/support",
                [PlatformPermissions.ViewSupport, PlatformPermissions.ManageSupport], DashboardAttentionKind.Information,
                "A customer has approved your time-limited support access. No approval decision is pending.", "Open approved support access");
        }
        return AppResponses.Success<IReadOnlyList<DashboardAttentionItemResponse>>(items);

        void Add(string key, string title, int count, string href, IReadOnlyList<string> permissions,
            DashboardAttentionKind kind, string reason, string actionLabel)
        {
            if (count > 0) items.Add(new DashboardAttentionItemResponse(key, title, count, href, permissions, kind, reason, actionLabel));
        }
    }
}
