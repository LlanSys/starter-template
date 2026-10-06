using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.Banking.Customers.Mappings;
using ST.Application.Features.HR.Employees.Mappings;
using ST.Application.Features.IAM.Users.Mappings;
using ST.Application.Features.Shared.EmailTemplates.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.SharedKernel.Features.HR.Employees.Dtos;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Features.HR.Employees.QueryHandlers;

// ── Get Active Employees (for RM dropdown) ────────────────────────────────


public record GetEmployeesQuery(string UserId) : IRequest<AppResponse<List<EmployeeResponse>>>, ICachableRequest
{
    public string CacheGroup => "employees";
    public string Discriminator => "all";
    public string? CacheUserId => null;
    public bool IsVersioned => true;
}
