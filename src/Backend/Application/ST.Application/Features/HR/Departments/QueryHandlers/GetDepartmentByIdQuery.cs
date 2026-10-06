using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.HR.Departments.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.HR.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.HR.Departments.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.HR.Departments.QueryHandlers;


public sealed record GetDepartmentByIdQuery(Guid Id, string UserId) : IRequest<AppResponse<DepartmentResponse>>, ICachableRequest
{
    public string CacheGroup => "departments";

    public string Discriminator => CacheKeys.Entity("departments", Id.ToString());

    public string? CacheUserId => null;

    public bool IsVersioned => true;
}
