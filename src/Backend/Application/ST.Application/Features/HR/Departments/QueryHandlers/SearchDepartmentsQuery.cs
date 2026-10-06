using System.Collections.ObjectModel;
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.HR.Departments.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.HR.Departments.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.HR.Departments.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.HR.Departments.QueryHandlers;


public sealed record SearchDepartmentsQuery(DepartmentSearchRequest SearchRequest, string UserId)
    : IRequest<AppResponse<PagedResponse<DepartmentResponse, Guid>>>, ICachableRequest
{
    public string CacheGroup => "departments";

    public string Discriminator => CacheKeys.Discriminator(SearchRequest);

    public string? CacheUserId => null;

    public bool IsVersioned => true;
}
