namespace ST.Infrastructure.Features.Shared.WorkTasks;

internal sealed class WorkTaskSource
{
    public required string Kind { get; init; }
    public Guid SubjectId { get; init; }
    public Guid CycleId { get; init; }
    public DateTimeOffset CycleAt { get; init; }
    public Guid TargetTenantId { get; init; }
    public required string SubjectLabel { get; init; }
}
