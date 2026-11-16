namespace ST.SharedKernel.Features.Shared.Dashboard;

public sealed record DashboardTaskKey(string Kind, Guid SubjectId, Guid CycleId, DateTimeOffset CycleAt);
