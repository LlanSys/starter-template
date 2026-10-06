using System.Collections.ObjectModel;
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.HR.Employees.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.HR.Employees.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.HR.Employees.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.HR.Employees.QueryHandlers;


public sealed record SearchEmployeesQuery(EmployeeSearchRequest SearchRequest, string UserId)
    : IRequest<AppResponse<PagedResponse<EmployeeResponse, Guid>>>, ICachableRequest
{
    public string CacheGroup => "employees";

    public string Discriminator => CacheKeys.Discriminator(SearchRequest);

    public string? CacheUserId => null;

    public bool IsVersioned => true;
}
