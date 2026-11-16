namespace ST.SharedKernel.Features.Shared.Dashboard;

public sealed record DashboardTaskPageResponse(IReadOnlyList<DashboardTaskResponse> Items, int Total,
    int Page, int PageSize, bool CanAssignOthers);
