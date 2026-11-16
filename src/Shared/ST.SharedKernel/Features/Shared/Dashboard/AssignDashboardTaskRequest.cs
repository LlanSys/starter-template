namespace ST.SharedKernel.Features.Shared.Dashboard;

public sealed record AssignDashboardTaskRequest(DashboardTaskKey Task, string? AssigneeUserId, Guid? ExpectedRevision);
