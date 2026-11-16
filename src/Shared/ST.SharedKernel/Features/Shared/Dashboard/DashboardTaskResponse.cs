namespace ST.SharedKernel.Features.Shared.Dashboard;

public sealed record DashboardTaskResponse(DashboardTaskKey Task, string SubjectLabel, string Href,
    string? AssigneeUserId, string? AssigneeName, bool? AssigneeEligible, Guid? Revision,
    bool CanClaim, bool CanRelease);
