using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.HR.Employees.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.HR.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.HR.Employees.Dtos;
using MediatR;

namespace ST.Application.Features.HR.Employees.QueryHandlers;


public sealed record GetEmployeeByIdQuery(Guid Id, string UserId)
    : IRequest<AppResponse<EmployeeResponse>>, ICachableRequest
{
    public string CacheGroup => "employees";

    public string Discriminator => CacheKeys.Entity("employees", Id.ToString());

    public string? CacheUserId => null;

    public bool IsVersioned => true;
}
