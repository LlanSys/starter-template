using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.HR.Employees.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.HR.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.HR.Employees.Dtos;
using ST.SharedKernel.Features.Shared.Phone;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.HR.Employees.CommandHandlers;


public sealed record UpdateEmployeeCommand(Guid Id, UpdateEmployeeRequest Request, string UserId)
    : IRequest<AppResponse<EmployeeResponse>>, ICacheInvalidatorRequest
{
    public IReadOnlyList<string> DirectInvalidationKeys => [CacheKeys.Entity("employees", Id.ToString())];

    public IReadOnlyList<string> GroupVersionKeysToInvalidate => [CacheKeys.GroupVersion("employees")];
}
