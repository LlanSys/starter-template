namespace ST.SharedKernel.Features.Shared.Dashboard.Dtos;

public sealed record DashboardSummaryResponse(int TotalAccounts, int ActiveAccounts, int PendingSetupAccounts, int DisabledAccounts);
