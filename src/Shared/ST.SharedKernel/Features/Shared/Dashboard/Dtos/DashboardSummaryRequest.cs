using System;
using System.Collections.Generic;
using System.Text;

namespace ST.SharedKernel.Features.Shared.Dashboard.Dtos;

public record DashboardSummaryRequest(string UserId, string? RoleScope = null);
