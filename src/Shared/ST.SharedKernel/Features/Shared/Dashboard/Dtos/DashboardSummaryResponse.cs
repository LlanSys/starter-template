using System;
using System.Collections.Generic;
using System.Text;

namespace ST.SharedKernel.Features.Shared.Dashboard.Dtos;

public record DashboardSummaryResponse(
    int TotalCustomers,
    int ActiveCustomers,
    int PendingApprovalCustomers,
    int DraftCustomers,
    IReadOnlyList<BreakdownGroup> BySegment,
    IReadOnlyList<BreakdownGroup> ByCustomerType,
    AgingData Aging,
    IReadOnlyList<RmWorkloadRow> RmWorkload);
