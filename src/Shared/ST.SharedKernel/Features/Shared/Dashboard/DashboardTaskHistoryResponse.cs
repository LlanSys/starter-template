namespace ST.SharedKernel.Features.Shared.Dashboard;

public sealed record DashboardTaskHistoryResponse(string Action, string ActorName, string? PreviousAssigneeName,
    string? AssigneeName, DateTimeOffset CreatedAt);
