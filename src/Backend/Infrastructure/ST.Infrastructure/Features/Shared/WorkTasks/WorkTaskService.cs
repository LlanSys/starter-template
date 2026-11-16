using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using ST.Application.Features.Shared.WorkTasks;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.ControlPlane.WorkTasks.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Features.IAM.DataContext;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Common.Enums;
using ST.SharedKernel.Features.Shared.Dashboard;

namespace ST.Infrastructure.Features.Shared.WorkTasks;

internal sealed partial class WorkTaskService(IControlPlaneUnitOfWork unit, WorkTaskSourceQuery sources,
    TaskAssigneeEligibility eligibility, ICurrentTenantProvider tenant, ICurrentActorProvider actor,
    IamDBContext iam, TimeProvider clock, ILogger<WorkTaskService> logger) : IWorkTaskService
{
    public async Task<DashboardTaskCounts> CountsAsync(string kind, CancellationToken cancellationToken)
    {
        var query = Rows(kind, clock.GetUtcNow());
        var mine = actor.ActorId;
        var unassigned = await query.CountAsync(item => item.AssigneeUserId == null, cancellationToken).ConfigureAwait(false);
        var assigned = await query.CountAsync(item => item.AssigneeUserId == mine, cancellationToken).ConfigureAwait(false);
        return new DashboardTaskCounts(unassigned, assigned);
    }

    public async Task<AppResponse<DashboardTaskPageResponse>> ListAsync(string kind, string queue, int page,
        int pageSize, bool canAssignOthers, CancellationToken cancellationToken)
    {
        if (!DashboardTaskCatalog.Definitions.TryGetValue(kind, out var definition) || queue is not ("mine" or "unassigned" or "all")
            || page < 1 || page > 100000 || pageSize < 1 || pageSize > 100)
            return AppResponses.Failure<DashboardTaskPageResponse>("Select a valid task category, queue and page.");

        if (queue == "all" && !canAssignOthers)
            return AppResponses.Failure<DashboardTaskPageResponse>(AppError.Forbidden("You cannot manage this team's assignments."));
        var query = Rows(kind, clock.GetUtcNow());
        var actorId = actor.ActorId;
        query = queue switch
        {
            "mine" => query.Where(item => item.AssigneeUserId == actorId),
            "unassigned" => query.Where(item => item.AssigneeUserId == null),
            _ => query
        };

        var count = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await query.OrderBy(item => item.SubjectLabel).ThenBy(item => item.SubjectId)
            .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(cancellationToken).ConfigureAwait(false);

        var ids = rows.Where(item => item.AssigneeUserId != null).Select(item => item.AssigneeUserId!).Distinct().ToArray();
        var names = await NamesAsync(ids, cancellationToken).ConfigureAwait(false);
        var eligible = ids.Length == 0 ? [] : await eligibility.FindAsync(definition, ids, null, cancellationToken).ConfigureAwait(false);
        var eligibleIds = eligible.Select(account => account.UserId).ToHashSet(StringComparer.Ordinal);
        var items = rows.Select(item => new DashboardTaskResponse(
            new DashboardTaskKey(kind, item.SubjectId, item.CycleId, item.CycleAt), item.SubjectLabel,
            Href(kind, item.TargetTenantId), item.AssigneeUserId,
            item.AssigneeUserId is null ? null : names.GetValueOrDefault(item.AssigneeUserId, "Unavailable account"),
            item.AssigneeUserId is null ? null : eligibleIds.Contains(item.AssigneeUserId), item.Revision,
            item.AssigneeUserId is null, item.AssigneeUserId is not null && (canAssignOthers || item.AssigneeUserId == actorId)))
            .ToArray();

        return AppResponses.Success(new DashboardTaskPageResponse(items, count, page, pageSize, canAssignOthers));
    }

    public async Task<AppResponse<bool>> AssignAsync(AssignDashboardTaskRequest request, bool canAssignOthers, CancellationToken cancellationToken)
    {
        var key = request.Task;
        var now = clock.GetUtcNow();
        var source = await Source(key, now).SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (source is null) return AppResponses.NotFound<bool>("This task is no longer available.");
        var record = await Records(key).AsTracking().SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        var reopened = record?.CompletedAt is not null;

        if ((reopened ? null : record?.Revision) != request.ExpectedRevision) return Conflict();
        var previous = reopened ? null : record?.AssigneeUserId;
        var actorId = actor.ActorId;
        var selfClaim = request.AssigneeUserId == actorId && (previous is null || previous == actorId);
        var selfRelease = request.AssigneeUserId is null && previous == actorId;

        if (!canAssignOthers && !selfClaim && !selfRelease)
            return AppResponses.Failure<bool>(AppError.Forbidden("You cannot change another person's assignment."));

        if (request.AssigneeUserId is { } target)
        {
            var candidates = await eligibility.FindAsync(DashboardTaskCatalog.Definitions[key.Kind], [target], null, cancellationToken).ConfigureAwait(false);

            if (candidates.Count != 1)
                return AppResponses.Failure<bool>("The assignee must be an active account in this workspace with authenticator MFA and every permission required to act on this task.");
        }

        if (previous == request.AssigneeUserId) return AppResponses.Success("The assignment is already up to date.", true);

        if (record is null)
        {
            record = new OperationalTaskAssignment(key.Kind, key.SubjectId, key.CycleId, key.CycleAt,
                tenant.TenantId, source.SubjectLabel, now);

            await unit.TaskAssignments.CreateAsync(record, cancellationToken).ConfigureAwait(false);
        }

        if (reopened)
        {
            var oldAssignee = record.AssigneeUserId;
            record.Reopen();
            await unit.TaskAssignmentEvents.CreateAsync(new OperationalTaskAssignmentEvent(record.Id, actorId, "Reopened",
                oldAssignee, null, now), cancellationToken).ConfigureAwait(false);
        }
        record.Assign(request.AssigneeUserId);
        var action = request.AssigneeUserId is null ? "Unassigned" : previous is not null ? "Reassigned" : selfClaim ? "Claimed" : "Assigned";
        await unit.TaskAssignmentEvents.CreateAsync(new OperationalTaskAssignmentEvent(record.Id, actorId, action,
            previous, request.AssigneeUserId, now), cancellationToken).ConfigureAwait(false);

        try
        {
            // Assignment and its event commit together. Revision and the unique source key fence concurrent claims.
            await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return AppResponses.Success(request.AssigneeUserId is null ? "Task returned to the team queue." : "Task assigned.", true);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            LogConflict(logger, exception);
            return Conflict();
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 }
            || exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            LogConflict(logger, exception);
            return Conflict();
        }
    }

    public async Task<AppResponse<IReadOnlyList<DashboardTaskAssigneeResponse>>> AssigneesAsync(DashboardTaskKey key,
        string? search, CancellationToken cancellationToken)
    {
        if (search?.Length > 100) return AppResponses.Failure<IReadOnlyList<DashboardTaskAssigneeResponse>>("Use a shorter account search.");

        if (!await Source(key, clock.GetUtcNow()).AnyAsync(cancellationToken).ConfigureAwait(false))
            return AppResponses.NotFound<IReadOnlyList<DashboardTaskAssigneeResponse>>("This task is no longer available.");
        return AppResponses.Success(await eligibility.FindAsync(DashboardTaskCatalog.Definitions[key.Kind], null, search, cancellationToken).ConfigureAwait(false));
    }

    public async Task<AppResponse<IReadOnlyList<DashboardTaskHistoryResponse>>> HistoryAsync(DashboardTaskKey key, CancellationToken cancellationToken)
    {
        var record = await Records(key).AsNoTracking().SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (record is null)
        {
            if (!await Source(key, clock.GetUtcNow()).AnyAsync(cancellationToken).ConfigureAwait(false))
                return AppResponses.NotFound<IReadOnlyList<DashboardTaskHistoryResponse>>("This task is unavailable.");
            return AppResponses.Success<IReadOnlyList<DashboardTaskHistoryResponse>>([]);
        }
        var events = await unit.TaskAssignmentEvents.FindAll().AsNoTracking().Where(item => item.AssignmentId == record.Id)
            .OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id).Take(100)
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);

        var ids = events.SelectMany(item => new[] { item.ActorId, item.PreviousAssigneeUserId, item.AssigneeUserId })
            .OfType<string>().Distinct().ToArray();

        var names = await NamesAsync(ids, cancellationToken).ConfigureAwait(false);
        string? Name(string? id) => id is null ? null : id == ICurrentActorProvider.SystemActor ? "System" : names.GetValueOrDefault(id, "Unavailable account");
        return AppResponses.Success<IReadOnlyList<DashboardTaskHistoryResponse>>(events.Select(item => new DashboardTaskHistoryResponse(
            item.Action, Name(item.ActorId)!, Name(item.PreviousAssigneeUserId), Name(item.AssigneeUserId), item.CreatedAt)).ToArray());
    }

    public async Task<Guid?> ReconcileAsync(string kind, Guid? after, CancellationToken cancellationToken)
    {
        var query = unit.TaskAssignments.FindAll().AsTracking().Where(item => item.Kind == kind && item.CompletedAt == null);

        if (after is { } cursor) query = query.Where(item => item.Id.CompareTo(cursor) > 0);
        var records = await query.OrderBy(item => item.Id).Take(100).ToArrayAsync(cancellationToken).ConfigureAwait(false);

        if (records.Length == 0) return null;
        var ids = records.Select(item => item.Id).ToArray();
        var now = clock.GetUtcNow();
        var open = sources.ForKind(kind, now, null);
        var existing = unit.TaskAssignments.FindAll().AsNoTracking().Where(item => ids.Contains(item.Id));
        var live = await (from source in open
                          join item in existing on new { source.Kind, source.SubjectId, source.CycleId, source.CycleAt }
                            equals new { item.Kind, item.SubjectId, item.CycleId, item.CycleAt }
                          select item.Id).ToArrayAsync(cancellationToken).ConfigureAwait(false);

        var liveIds = live.ToHashSet();

        foreach (var item in records.Where(item => !liveIds.Contains(item.Id)))
        {
            item.Complete(now);
            await unit.TaskAssignmentEvents.CreateAsync(new OperationalTaskAssignmentEvent(item.Id, ICurrentActorProvider.SystemActor,
                "Resolved", item.AssigneeUserId, item.AssigneeUserId, now), cancellationToken).ConfigureAwait(false);
        }
        await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
        return records[^1].Id;
    }

    private IQueryable<WorkTaskSource> Source(DashboardTaskKey key, DateTimeOffset now)
    {
        return sources.ForKind(key.Kind, now, tenant.TenantId).Where(item => item.SubjectId == key.SubjectId
            && item.CycleId == key.CycleId && item.CycleAt == key.CycleAt);
    }

    private IQueryable<OperationalTaskAssignment> Records(DashboardTaskKey key)
    {
        var owner = tenant.TenantId;
        return unit.TaskAssignments.FindAll().Where(item => item.OwnerTenantId == owner && item.Kind == key.Kind
            && item.SubjectId == key.SubjectId && item.CycleId == key.CycleId && item.CycleAt == key.CycleAt);
    }

    private IQueryable<WorkTaskRow> Rows(string kind, DateTimeOffset now)
    {
        var owner = tenant.TenantId;
        var open = sources.ForKind(kind, now, owner);
        var assignments = unit.TaskAssignments.FindAll().AsNoTracking().Where(item => item.OwnerTenantId == owner && item.CompletedAt == null);
        return from source in open
               join assignment in assignments on new { source.Kind, source.SubjectId, source.CycleId, source.CycleAt }
                 equals new { assignment.Kind, assignment.SubjectId, assignment.CycleId, assignment.CycleAt } into matches
               from assignment in matches.DefaultIfEmpty()
               select new WorkTaskRow { SubjectId = source.SubjectId, CycleId = source.CycleId, CycleAt = source.CycleAt,
                   TargetTenantId = source.TargetTenantId, SubjectLabel = source.SubjectLabel,
                   AssigneeUserId = assignment == null ? null : assignment.AssigneeUserId,
                   Revision = assignment == null ? null : assignment.Revision };
    }

    private async Task<Dictionary<string, string>> NamesAsync(string[] ids, CancellationToken cancellationToken)
    {
        var owner = tenant.TenantId;
        return await iam.Users.IgnoreQueryFilters().AsNoTracking().Where(user => user.TenantId == owner && ids.Contains(user.Id))
            .Select(user => new { user.Id, Name = user.FirstName + " " + user.LastName + " (" + (user.UserName ?? "Account") + ")" })
            .ToDictionaryAsync(user => user.Id, user => user.Name, cancellationToken).ConfigureAwait(false);
    }

    private static string Href(string kind, Guid targetTenant)
    {
        return kind switch
        {
            "kyc-review" => $"/control-panel/tenants/{targetTenant:D}/kyc",
            "hold-review" => $"/control-panel/kyc/retention?TenantId={targetTenant:D}",
            "support-approval" => "/admin/support",
            _ => $"/control-panel/tenants/{targetTenant:D}"
        };
    }

    private static AppResponse<bool> Conflict()
    {
        return AppResponses.Failure<bool>(new AppError(ErrorType.Conflict, "TASK_ASSIGNMENT_CHANGED", "This task changed. Refresh the queue before assigning it."));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Concurrent task assignment rejected; reload the task before retrying.")]
    private static partial void LogConflict(ILogger logger, Exception exception);
}
