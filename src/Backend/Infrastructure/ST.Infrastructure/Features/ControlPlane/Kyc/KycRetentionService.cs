using Azure;
using Microsoft.Extensions.Logging;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ST.Application.Features.ControlPlane.Kyc.Contracts;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Kyc.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Domain.Shared.Contracts.Common;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;

namespace ST.Infrastructure.Features.ControlPlane.Kyc;

internal sealed partial class KycRetentionService(IControlPlaneUnitOfWork unit, IKycEvidenceLifecycleStorage storage,
    ICurrentActorProvider actor, TimeProvider clock,
    IValidator<PublishKycRetentionPolicyRequest> policyValidator, IValidator<KycRetentionActionRequest> actionValidator, ILogger<KycRetentionService> logger, KycOrphanScanProgress scan) : IKycRetentionService
{
    public async Task<AppResponse<bool>> CheckErasureAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        try
        {
            return AppResponses.Success(await storage.IsErasureRecordedAsync($"cases/{tenantId:N}", cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception) when (exception is RequestFailedException or HttpRequestException or IOException)
        {
            LogStorageFailure(logger, exception);
            return AppResponses.Failure<bool>(AppError.DependencyUnavailable("KYC evidence status is temporarily unavailable. Try again."));
        }
    }

    public async Task<KycRetentionCase> EnsureAsync(Guid tenantId, bool approved, CancellationToken cancellationToken)
    {
        var item = await unit.KycRetentionCases.FindAll().AsTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken).ConfigureAwait(false);

        if (item is not null) return item;
        var selection = await unit.KycRetentionPolicySelection.FindAll().SingleAsync(cancellationToken).ConfigureAwait(false);
        var policy = await PolicyAsync(selection.PolicyId, cancellationToken).ConfigureAwait(false);
        item = new KycRetentionCase(tenantId, policy.Id, clock.GetUtcNow());
        item.Schedule(policy, approved);
        await unit.KycRetentionCases.CreateAsync(item, cancellationToken).ConfigureAwait(false);
        // The caller commits this with its evidence write, so case activity and upload completion are atomic.
        return item;
    }

    public async Task<AppResponse<KycRetentionStatusResponse>> StatusAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await unit.Tenants.FindByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);

        if (tenant is null || !tenant.IsCustomerWorkspace) return AppResponses.NotFound<KycRetentionStatusResponse>("Workspace not found.");
        var item = await EnsureAsync(tenantId, tenant.KycApprovedAt.HasValue, cancellationToken).ConfigureAwait(false);
        await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
        return AppResponses.Success(new KycRetentionStatusResponse(item.TenantId, item.PolicyId, item.Revision,
            item.Outcome, item.Phase, item.LastActivityAt, item.ClosedAt, item.RetainUntil, item.RecoveryUntil,
            item.Held, item.HoldReviewDue, item.FailureCode, item.NextAttemptAt));
    }

    public async Task<AppResponse<IReadOnlyList<KycRetentionPolicyResponse>>> PoliciesAsync(CancellationToken cancellationToken)
    {
        var selected = await unit.KycRetentionPolicySelection.FindAll().SingleAsync(cancellationToken).ConfigureAwait(false);
        var policies = await unit.KycRetentionPolicies.FindAll().OrderByDescending(x => x.Id == selected.PolicyId)
            .ThenByDescending(x => x.PublishedAt).Take(100).ToArrayAsync(cancellationToken).ConfigureAwait(false);

        IReadOnlyList<KycRetentionPolicyResponse> result = policies.Select(x => new KycRetentionPolicyResponse(x.Id, x.Name,
            x.ApprovedMonths, x.ClosedDays, x.InactiveDays, x.RecoveryDays, x.OrphanDays, x.HoldReviewDays,
            x.PublishedAt, x.PublishedBy, x.Reason)).ToArray();

        return AppResponses.Success(result);
    }

    public async Task<AppResponse<Guid>> PublishAsync(PublishKycRetentionPolicyRequest request, CancellationToken cancellationToken)
    {
        if (!(await policyValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false)).IsValid)
            return AppResponses.Failure<Guid>("Provide a policy name, reason and valid positive retention periods.");
        var selection = await unit.KycRetentionPolicySelection.FindAll().AsTracking().SingleAsync(cancellationToken).ConfigureAwait(false);

        if (selection.PolicyId != request.ExpectedPolicyId) return AppResponses.Failure<Guid>("The active policy changed. Refresh before publishing.");
        var policy = new KycRetentionPolicy
        {
            Name = request.Name.Trim(),
            ApprovedMonths = request.ApprovedMonths,
            ClosedDays = request.ClosedDays,
            InactiveDays = request.InactiveDays,
            RecoveryDays = request.RecoveryDays,
            OrphanDays = request.OrphanDays,
            HoldReviewDays = request.HoldReviewDays,
            PublishedAt = clock.GetUtcNow(),
            PublishedBy = actor.ActorId,
            Reason = request.Reason.Trim()
        };

        await unit.KycRetentionPolicies.CreateAsync(policy, cancellationToken).ConfigureAwait(false);
        selection.PolicyId = policy.Id;
        selection.Revision = Guid.CreateVersion7();
        await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
        return AppResponses.Success(policy.Id);
    }

    public async Task<AppResponse<bool>> ActAsync(Guid tenantId, KycRetentionActionRequest request, CancellationToken cancellationToken)
    {
        if (!(await actionValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false)).IsValid) return AppResponses.Failure<bool>("Enter a reason of 1–1000 characters, without document contents.");
        var tenant = await unit.Tenants.FindByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);

        if (tenant is null || !tenant.IsCustomerWorkspace) return AppResponses.NotFound<bool>("Workspace not found.");
        var item = await EnsureAsync(tenantId, tenant.KycApprovedAt.HasValue, cancellationToken).ConfigureAwait(false);

        if (item.Revision != request.ExpectedRevision) return AppResponses.Failure<bool>("This case changed. Refresh before continuing.");
        var policy = await PolicyAsync(item.PolicyId, cancellationToken).ConfigureAwait(false);
        var now = clock.GetUtcNow();

        if (request.Action is "hold" or "restore" && await storage.IsErasureRecordedAsync($"cases/{tenantId:N}", cancellationToken).ConfigureAwait(false))
            return AppResponses.Failure<bool>("This case has a committed erasure record. Restoring an older database cannot reopen it.");

        if (request.Action == "hold" && await unit.KycUploadReservations.AnyAsync(x => x.TenantId == tenantId && x.State == "Deleting", cancellationToken).ConfigureAwait(false))
            return AppResponses.Failure<bool>("An unattached upload deletion has already committed. Wait for it to complete before placing a hold.");

        try
        {
            switch (request.Action)
            {
                case "hold": item.SetHold(true, now, policy); break;
                case "release": item.SetHold(false, now, policy); break;
                case "restore": item.Restore(now, policy, tenant.KycApprovedAt.HasValue); break;
                case "apply-current-policy":
                    var selection = await unit.KycRetentionPolicySelection.FindAll().SingleAsync(cancellationToken).ConfigureAwait(false);
                    item.ApplyPolicy(await PolicyAsync(selection.PolicyId, cancellationToken).ConfigureAwait(false), tenant.KycApprovedAt.HasValue); break;
                case "Declined":
                case "Withdrawn":
                case "Ended":
                    if (request.Action == "Ended" && tenant.Status != TenantStatus.Suspended)
                        return AppResponses.Failure<bool>("Suspend the workspace before recording the end of its customer relationship.");
                    item.Close(request.Action, now, policy, tenant.KycApprovedAt.HasValue); break;
                default: return AppResponses.Failure<bool>("This retention action is unavailable.");
            }
        }
        catch (InvalidOperationException exception)
        {
            return AppResponses.Failure<bool>(exception.Message); // Controlled domain messages only.
        }
        await AuditAsync(item, request.Action, request.Reason.Trim(), actor.ActorId, now, cancellationToken).ConfigureAwait(false);
        await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
        return AppResponses.Success(true);
    }

    public async Task ProcessAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await unit.Tenants.FindByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);

        if (tenant is null || !tenant.IsCustomerWorkspace) return;
        var item = await EnsureAsync(tenantId, tenant.KycApprovedAt.HasValue, cancellationToken).ConfigureAwait(false);
        var policy = await PolicyAsync(item.PolicyId, cancellationToken).ConfigureAwait(false);
        var now = clock.GetUtcNow();

        if (item.Phase == "Retained")
        {
            // Approval and upload both touch this row, fencing expiry against concurrent KYC activity.
            item.Schedule(policy, tenant.KycApprovedAt.HasValue);

            if (!item.Held && item.RetainUntil.HasValue && item.RetainUntil <= now)
            {
                item.BeginRecovery(now, policy);
                await AuditAsync(item, "recovery-started", "Retention period elapsed.", SystemActor, now, cancellationToken).ConfigureAwait(false);
            }
            await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        if (item.Held || item.Phase == "Erased" || item.RecoveryUntil > now || item.LeaseUntil > now || item.NextAttemptAt > now) return;
        item.ClaimDeletion(now);
        await AuditAsync(item, "deletion-committed", "Recovery period elapsed.", SystemActor, now, cancellationToken).ConfigureAwait(false);
        await unit.CompleteAsync(cancellationToken).ConfigureAwait(false); // Linearization point: later holds cannot undo external deletion.
        var documents = await unit.KycDocuments.FindAll().AsTracking().Where(x => x.TenantId == tenantId && x.ErasedAt == null)
            .OrderBy(x => x.Id).ToArrayAsync(cancellationToken).ConfigureAwait(false);

        var allErased = true;
        string? failureCode = null;

        try
        {
            await storage.RecordErasureAsync($"cases/{tenantId:N}", cancellationToken).ConfigureAwait(false);

            foreach (var document in documents)
            {
                await storage.RecordErasureAsync(document.StorageKey, cancellationToken).ConfigureAwait(false);

                if (!await storage.PurgeAsync(document.StorageKey, cancellationToken).ConfigureAwait(false))
                {
                    allErased = false;
                    continue;
                }
                document.EraseMetadata(now);
                // Review notes are evidence too; minimal lifecycle receipts remain separately.
                await unit.KycDocumentReviews.FindAll().Where(x => x.DocumentId == document.Id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
                await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is RequestFailedException or HttpRequestException or IOException)
        {
            LogStorageFailure(logger, exception);
            allErased = false;
            failureCode = exception is RequestFailedException storageError && storageError.Status is 409 or 412
                ? "StorageProtected" : "StorageUnavailable";
        }
        item.FinishAttempt(allErased, allErased ? null : failureCode ?? "ProviderRetention", clock.GetUtcNow());

        if (allErased)
        {
            // Preserve the minimal approval decision, but remove any free-text evidence reference as well as review notes.
            await unit.Tenants.FindAll().Where(x => x.Id == tenantId && x.KycEvidenceReference != null)
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.KycEvidenceReference, "Evidence erased under the retention policy.")
                    .SetProperty(x => x.UpdatedAt, now).SetProperty(x => x.UpdatedBy, SystemActor), cancellationToken).ConfigureAwait(false);

            await unit.KycSubmissionLinks.FindAll().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            await unit.KycRetentionAudit.FindAll().Where(x => x.TenantId == tenantId)
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.Reason, "Private case notes removed after evidence erasure."), cancellationToken).ConfigureAwait(false);
        }
        await AuditAsync(item, allErased ? "erased" : "provider-retention", "Deletion attempt completed.", SystemActor, now, cancellationToken).ConfigureAwait(false);
        await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ReconcileOrphansAsync(CancellationToken cancellationToken)
    {
        var selection = await unit.KycRetentionPolicySelection.FindAll().SingleAsync(cancellationToken).ConfigureAwait(false);
        var policy = await PolicyAsync(selection.PolicyId, cancellationToken).ConfigureAwait(false);
        var now = clock.GetUtcNow();
        // A deleted current blob disappears from normal inventory. Retry durable orphan intents independently of that inventory.
        var retryJobs = await unit.KycUploadReservations.FindAll().AsTracking()
            .Where(x => x.State == "Deleting" && x.LeaseUntil <= now).OrderBy(x => x.LeaseUntil).Take(25)
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);

        foreach (var job in retryJobs)
        {
            if (await unit.KycDocuments.AnyAsync(x => x.Id == job.Id, cancellationToken).ConfigureAwait(false)) continue;
            job.LeaseUntil = now.AddMinutes(5);
            job.Revision = Guid.CreateVersion7();
            await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
            await FinishOrphanAsync(job, now, cancellationToken).ConfigureAwait(false);
        }
        var page = await storage.ListAsync(scan.Cursor, cancellationToken).ConfigureAwait(false);
        var owned = new List<(KycStoredObject Blob, Guid TenantId, Guid Id)>();

        foreach (var blob in page.Items)
        {
            var parts = blob.Key.Split('/');

            if (parts.Length == 3 && parts[0] == "kyc" && Guid.TryParseExact(parts[1], "N", out var tenantId)
                && Guid.TryParseExact(parts[2], "N", out var id) && blob.LastModified <= now.AddDays(-policy.OrphanDays))
                owned.Add((blob, tenantId, id));
        }
        var ids = owned.Select(x => x.Id).ToArray();
        var tenants = owned.Select(x => x.TenantId).Distinct().ToArray();
        var existing = (await unit.KycDocuments.FindAll().Where(x => ids.Contains(x.Id)).Select(x => x.Id)
            .ToArrayAsync(cancellationToken).ConfigureAwait(false)).ToHashSet();

        var cases = await unit.KycRetentionCases.FindAll().AsTracking().Where(x => tenants.Contains(x.TenantId))
            .ToDictionaryAsync(x => x.TenantId, cancellationToken).ConfigureAwait(false);

        var reservations = await unit.KycUploadReservations.FindAll().AsTracking().Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken).ConfigureAwait(false);

        foreach (var candidate in owned)
        {
            if (existing.Contains(candidate.Id)) continue;
            cases.TryGetValue(candidate.TenantId, out var retentionCase);

            if (retentionCase?.Held == true) continue;
            reservations.TryGetValue(candidate.Id, out var reservation);

            if (reservation is not null && reservation.LeaseUntil > now) continue;
            // Recheck only apparent orphans after the batched read. Final upload metadata and reservation revision commit atomically.
            if (await unit.KycDocuments.AnyAsync(x => x.Id == candidate.Id, cancellationToken).ConfigureAwait(false)) continue;

            if (retentionCase is null)
            {
                var tenant = await unit.Tenants.FindByIdAsync(candidate.TenantId, cancellationToken).ConfigureAwait(false);

                if (tenant?.IsCustomerWorkspace == true)
                {
                    retentionCase = await EnsureAsync(tenant.Id, tenant.KycApprovedAt.HasValue, cancellationToken).ConfigureAwait(false);
                    cases[tenant.Id] = retentionCase;
                }
            }

            if (retentionCase?.Held == true) continue;
            retentionCase?.FenceOrphanDeletion();

            if (reservation is null)
            {
                reservation = new KycUploadReservation { Id = candidate.Id, TenantId = candidate.TenantId };
                await unit.KycUploadReservations.CreateAsync(reservation, cancellationToken).ConfigureAwait(false);
            }
            reservation.State = "Deleting";
            reservation.LeaseUntil = now.AddMinutes(5);
            reservation.Revision = Guid.CreateVersion7();
            await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
            await FinishOrphanAsync(reservation, now, cancellationToken).ConfigureAwait(false);
        }
        scan.Cursor = page.NextCursor;
    }

    private async Task FinishOrphanAsync(KycUploadReservation job, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var key = $"kyc/{job.TenantId:N}/{job.Id:N}";
        await storage.RecordErasureAsync(key, cancellationToken).ConfigureAwait(false);

        if (await storage.PurgeAsync(key, cancellationToken).ConfigureAwait(false)) job.State = "Erased";
        job.LeaseUntil = now.AddHours(1);
        job.Revision = Guid.CreateVersion7();
        await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
    }

    private const string SystemActor = "00000000-0000-0000-0000-000000000001";
    private Task<KycRetentionPolicy> PolicyAsync(Guid id, CancellationToken cancellationToken) =>
        unit.KycRetentionPolicies.FindAll().SingleAsync(x => x.Id == id, cancellationToken);

    private async Task AuditAsync(KycRetentionCase item, string action, string reason, string actorId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await unit.KycRetentionAudit.CreateAsync(new KycRetentionAudit
        {
            TenantId = item.TenantId,
            PolicyId = item.PolicyId,
            Action = action,
            Reason = reason,
            ActorId = actorId,
            OccurredAt = now
        }, cancellationToken).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "KYC storage erasure could not complete; a retry has been scheduled.")]
    private static partial void LogStorageFailure(ILogger logger, Exception exception);
}
