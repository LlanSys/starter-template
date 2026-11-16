namespace ST.Domain.Features.ControlPlane.WorkTasks.Entities;

public sealed class OperationalTaskAssignment
{
    private OperationalTaskAssignment() { }

    public OperationalTaskAssignment(string kind, Guid subjectId, Guid cycleId, DateTimeOffset cycleAt,
        Guid ownerTenantId, string subjectLabel, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);

        if (subjectId == Guid.Empty || ownerTenantId == Guid.Empty) throw new ArgumentException("A task requires a subject and workspace.");
        Kind = kind;
        SubjectId = subjectId;
        CycleId = cycleId;
        CycleAt = cycleAt;
        OwnerTenantId = ownerTenantId;
        SubjectLabel = subjectLabel;
        CreatedAt = now;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public string Kind { get; private set; } = string.Empty;
    public Guid SubjectId { get; private set; }
    public Guid CycleId { get; private set; }
    public DateTimeOffset CycleAt { get; private set; }
    public Guid OwnerTenantId { get; private set; }
    public string SubjectLabel { get; private set; } = string.Empty;
    public string? AssigneeUserId { get; private set; }
    public Guid Revision { get; private set; } = Guid.CreateVersion7();
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public bool Assign(string? assigneeUserId)
    {
        if (CompletedAt.HasValue) throw new InvalidOperationException("This task is resolved.");

        if (assigneeUserId is not null && (string.IsNullOrWhiteSpace(assigneeUserId) || assigneeUserId.Length > 128))
            throw new ArgumentException("Select a valid assignee.", nameof(assigneeUserId));

        if (AssigneeUserId == assigneeUserId) return false;
        AssigneeUserId = assigneeUserId;
        Revision = Guid.CreateVersion7();
        return true;
    }

    public void Reopen()
    {
        if (!CompletedAt.HasValue) throw new InvalidOperationException("This task is already open.");
        CompletedAt = null;
        AssigneeUserId = null;
        Revision = Guid.CreateVersion7();
    }

    public bool Complete(DateTimeOffset now)
    {
        if (CompletedAt.HasValue) return false;
        CompletedAt = now;
        Revision = Guid.CreateVersion7();
        return true;
    }
}
