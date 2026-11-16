namespace ST.Domain.Features.IAM.PlatformStaff.Entities;

public sealed class PlatformStaffChange
{
    private PlatformStaffChange()
    {
    }

    public PlatformStaffChange(
        Guid tenantId,
        string userId,
        string actorId,
        string action,
        string reason,
        bool previousStaff,
        bool currentStaff,
        bool previousActive,
        bool currentActive,
        string previousPermissionsJson,
        string currentPermissionsJson,
        DateTimeOffset now
    )
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("A staff audit requires its platform workspace.", nameof(tenantId));
        }

        TenantId = tenantId;
        UserId = userId;
        ActorId = actorId;
        Action = action;
        Reason = reason;
        PreviousStaff = previousStaff;
        CurrentStaff = currentStaff;
        PreviousActive = previousActive;
        CurrentActive = currentActive;
        PreviousPermissionsJson = previousPermissionsJson;
        CurrentPermissionsJson = currentPermissionsJson;
        OccurredAt = now;
    }

    public Guid TenantId { get; private set; }
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public string UserId { get; private set; } = string.Empty;
    public string ActorId { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public bool PreviousStaff { get; private set; }
    public bool CurrentStaff { get; private set; }
    public bool PreviousActive { get; private set; }
    public bool CurrentActive { get; private set; }
    public string PreviousPermissionsJson { get; private set; } = "[]";
    public string CurrentPermissionsJson { get; private set; } = "[]";
    public DateTimeOffset OccurredAt { get; private set; }
}
