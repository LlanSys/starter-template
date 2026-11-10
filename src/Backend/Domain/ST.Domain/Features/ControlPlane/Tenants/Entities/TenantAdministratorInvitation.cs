using ST.Domain.Features.ControlPlane.Tenants.Enums;

namespace ST.Domain.Features.ControlPlane.Tenants.Entities;

public sealed class TenantAdministratorInvitation
{
    private TenantAdministratorInvitation() { }

    public TenantAdministratorInvitation(Guid tenantId, string email, string firstName, string lastName, string actor, DateTimeOffset now)
    {
        TenantId = tenantId;
        Email = email.Trim();
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        CreatedBy = actor;
        CreatedAt = now;
        NextAttemptAt = now;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid TenantId { get; private set; }
    public Tenant Tenant { get; private set; } = null!;
    public string Email { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid Revision { get; private set; } = Guid.CreateVersion7();
    public AdministratorInvitationState State { get; private set; }
    public string? TokenHash { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? LeaseUntil { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public int Attempts { get; private set; }
    public string? ClaimedUserId { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? ChangedBy { get; private set; }

    public bool TryIssue(string tokenHash, DateTimeOffset now, TimeSpan lifetime)
    {
        if (State != AdministratorInvitationState.Pending || DeliveredAt.HasValue || LeaseUntil > now || NextAttemptAt > now)
        {
            return false;
        }

        TokenHash = tokenHash;
        ExpiresAt = now.Add(lifetime);
        LeaseUntil = now.AddMinutes(5);
        Attempts++;
        Touch();
        return true;
    }

    public void ReportDelivery(bool sent, DateTimeOffset now)
    {
        DeliveredAt = sent ? now : null;
        LeaseUntil = null;
        NextAttemptAt = now.AddMinutes(1 << Math.Min(Attempts, 6));
        Touch();
    }

    public bool Resend(string actor, DateTimeOffset now)
    {
        if (State is not (AdministratorInvitationState.Pending or AdministratorInvitationState.Revoked) || LeaseUntil > now)
        {
            return false;
        }

        State = AdministratorInvitationState.Pending;
        RevokedAt = null;
        TokenHash = null;
        ExpiresAt = null;
        DeliveredAt = null;
        NextAttemptAt = now;
        Attempts = 0;
        ChangedBy = actor;
        Touch();
        return true;
    }

    public bool Reserve(string userId, DateTimeOffset now)
    {
        if (State != AdministratorInvitationState.Pending || ExpiresAt <= now
            || (ClaimedUserId is not null && ClaimedUserId != userId))
        {
            return false;
        }

        if (ClaimedUserId is null)
        {
            ClaimedUserId = userId;
            Touch();
        }
        return true;
    }

    public void Claim()
    {
        State = AdministratorInvitationState.Claimed;
        TokenHash = null;
        LeaseUntil = null;
        Touch();
    }

    public bool Accept(string userId, DateTimeOffset now)
    {
        if (State != AdministratorInvitationState.Claimed || ClaimedUserId != userId)
        {
            return false;
        }

        State = AdministratorInvitationState.Accepted;
        AcceptedAt = now;
        ChangedBy = userId;
        Touch();
        return true;
    }

    public bool Revoke(string actor, DateTimeOffset now)
    {
        if (State is AdministratorInvitationState.Accepted or AdministratorInvitationState.Revoked)
        {
            return false;
        }

        State = AdministratorInvitationState.Revoked;
        RevokedAt = now;
        ChangedBy = actor;
        TokenHash = null;
        LeaseUntil = null;
        Touch();
        return true;
    }

    private void Touch()
    {
        Revision = Guid.CreateVersion7();
    }
}
