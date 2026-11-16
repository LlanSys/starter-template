namespace ST.SharedKernel.Features.Shared.Dashboard;

public sealed record DashboardAttentionItemResponse(
    string Key,
    string Title,
    int Count,
    string Href,
    IReadOnlyList<string> RequiredPermissions,
    DashboardAttentionKind Kind,
    string Reason,
    string ActionLabel);
