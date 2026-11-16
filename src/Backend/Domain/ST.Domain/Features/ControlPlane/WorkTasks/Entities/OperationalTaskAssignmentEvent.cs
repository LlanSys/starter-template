namespace ST.Domain.Features.ControlPlane.WorkTasks.Entities;

public sealed class OperationalTaskAssignmentEvent
{
    private OperationalTaskAssignmentEvent() { }

    public OperationalTaskAssignmentEvent(Guid assignmentId, string actorId, string action,
        string? previousAssigneeUserId, string? assigneeUserId, DateTimeOffset now)
    {
        AssignmentId = assignmentId;
        ActorId = actorId;
        Action = action;
        PreviousAssigneeUserId = previousAssigneeUserId;
        AssigneeUserId = assigneeUserId;
        CreatedAt = now;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid AssignmentId { get; private set; }
    public string ActorId { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string? PreviousAssigneeUserId { get; private set; }
    public string? AssigneeUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
