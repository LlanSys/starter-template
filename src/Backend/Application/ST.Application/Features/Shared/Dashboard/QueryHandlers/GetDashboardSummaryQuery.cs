using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.Banking.Customers.Enums;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Dashboard.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using ST.SharedKernel.Extensions;

namespace ST.Application.Features.Shared.Dashboard.QueryHandlers;


public record GetDashboardSummaryQuery(string UserId, string? RoleScope = null) : IRequest<AppResponse<DashboardSummaryResponse>>, ICachableRequest

{
    public string CacheGroup => "dashboard";
    public string Discriminator => CacheKeys.Discriminator(new { RoleScope });
    public string? CacheUserId => null;
    public bool IsVersioned => true;
    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}
