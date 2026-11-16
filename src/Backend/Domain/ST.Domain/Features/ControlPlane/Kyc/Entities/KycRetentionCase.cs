namespace ST.Domain.Features.ControlPlane.Kyc.Entities;

public sealed class KycRetentionCase
{
    private KycRetentionCase() { }

    public KycRetentionCase(Guid tenantId, Guid policyId, DateTimeOffset now)
    {
        TenantId = tenantId;
        PolicyId = policyId;
        LastActivityAt = now;
    }

    public Guid TenantId { get; private set; }
    public Guid PolicyId { get; private set; }
    public string Outcome { get; private set; } = "Open";
    public DateTimeOffset LastActivityAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public DateTimeOffset? RetainUntil { get; private set; }
    public string Phase { get; private set; } = "Retained";
    public DateTimeOffset? RecoveryUntil { get; private set; }
    public bool Held { get; private set; }
    public DateTimeOffset? HoldReviewDue { get; private set; }
    public Guid Revision { get; private set; } = Guid.CreateVersion7();
    public Guid? LeaseId { get; private set; }
    public DateTimeOffset? LeaseUntil { get; private set; }
    public DateTimeOffset? NextAttemptAt { get; private set; }
    public string? FailureCode { get; private set; }

    public bool CanCollect => Outcome == "Open" && Phase == "Retained";

    public void Touch(DateTimeOffset now)
    {
        if (!CanCollect) throw new InvalidOperationException("This KYC case is closed.");
        LastActivityAt = now;
        Change();
    }

    public void Schedule(KycRetentionPolicy policy, bool approved)
    {
        RetainUntil = Outcome switch
        {
            "Ended" => ClosedAt!.Value.AddMonths(policy.ApprovedMonths),
            "Declined" or "Withdrawn" => ClosedAt!.Value.AddDays(policy.ClosedDays),
            "Open" when !approved => LastActivityAt.AddDays(policy.InactiveDays),
            _ => null
        };
        Change();
    }

    public void Close(string outcome, DateTimeOffset now, KycRetentionPolicy policy, bool approved)
    {
        if (Phase != "Retained" || Outcome != "Open") throw new InvalidOperationException("This case is already closed.");
        if (approved != (outcome == "Ended") || outcome is not ("Ended" or "Declined" or "Withdrawn"))
            throw new InvalidOperationException("Choose the closure appropriate to the KYC decision.");
        Outcome = outcome;
        ClosedAt = now;
        Schedule(policy, approved);
    }

    public void SetHold(bool held, DateTimeOffset now, KycRetentionPolicy policy)
    {
        if (Phase is "Deleting" or "Erased") throw new InvalidOperationException("Deletion has already committed; remaining provider copies cannot be promised recoverable.");
        Held = held;
        HoldReviewDue = held ? now.AddDays(policy.HoldReviewDays) : null;
        Change();
    }

    public void ApplyPolicy(KycRetentionPolicy policy, bool approved)
    {
        if (Phase != "Retained") throw new InvalidOperationException("Restore the case before changing its schedule.");
        PolicyId = policy.Id;
        Schedule(policy, approved);
    }

    public void BeginRecovery(DateTimeOffset now, KycRetentionPolicy policy)
    {
        if (Held || Phase != "Retained" || !RetainUntil.HasValue || RetainUntil > now)
            throw new InvalidOperationException("Evidence is not due for deletion.");
        if (Outcome == "Open") { Outcome = "Abandoned"; ClosedAt = RetainUntil; }
        Phase = "Recovery";
        RecoveryUntil = now.AddDays(policy.RecoveryDays);
        Change();
    }

    public void Restore(DateTimeOffset now, KycRetentionPolicy policy, bool approved)
    {
        if (Phase != "Recovery") throw new InvalidOperationException("Only recoverable evidence can be restored.");
        Phase = "Retained";
        Outcome = "Open";
        ClosedAt = null;
        RecoveryUntil = null;
        LastActivityAt = now;
        Schedule(policy, approved);
    }

    public void ClaimDeletion(DateTimeOffset now)
    {
        if (Held || (Phase != "Recovery" && Phase != "Deleting") || RecoveryUntil > now || LeaseUntil > now)
            throw new InvalidOperationException("This deletion is unavailable.");
        Phase = "Deleting";
        LeaseId = Guid.CreateVersion7();
        LeaseUntil = now.AddMinutes(5);
        NextAttemptAt = now.AddMinutes(10);
        Change();
    }

    public void FinishAttempt(bool erased, string? failureCode, DateTimeOffset now)
    {
        Phase = erased ? "Erased" : "Deleting";
        FailureCode = failureCode;
        LeaseId = null;
        LeaseUntil = null;
        NextAttemptAt = erased ? null : now.AddHours(1);
        Change();
    }

    public void FenceOrphanDeletion()
    {
        if (Held) throw new InvalidOperationException("Legal hold blocks orphan deletion.");
        Change();
    }

    private void Change() { Revision = Guid.CreateVersion7(); }
}
