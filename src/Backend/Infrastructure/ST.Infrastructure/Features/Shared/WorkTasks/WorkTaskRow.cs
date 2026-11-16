namespace ST.Infrastructure.Features.Shared.WorkTasks;

internal sealed class WorkTaskRow
{
    public Guid SubjectId { get; init; }
    public Guid CycleId { get; init; }
    public DateTimeOffset CycleAt { get; init; }
    public Guid TargetTenantId { get; init; }
    public required string SubjectLabel { get; init; }
    public string? AssigneeUserId { get; init; }
    public Guid? Revision { get; init; }
}
