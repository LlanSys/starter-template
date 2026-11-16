namespace ST.SharedKernel.Features.Shared.Dashboard;

public sealed record DashboardTaskDefinition(string Kind, string Title, string Reason, string ActionLabel,
    bool IsPlatform, IReadOnlyList<string> RequiredPermissions);
