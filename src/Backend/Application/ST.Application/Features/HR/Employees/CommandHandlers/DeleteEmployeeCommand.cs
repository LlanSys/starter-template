using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.Domain.Features.HR.Contracts;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.HR.Employees.CommandHandlers;


public sealed record DeleteEmployeeCommand(Guid Id, string UserId)
    : IRequest<AppResponse<bool>>, ICacheInvalidatorRequest
{
    public IReadOnlyList<string> DirectInvalidationKeys => [CacheKeys.Entity("employees", Id.ToString())];

    public IReadOnlyList<string> GroupVersionKeysToInvalidate => [CacheKeys.GroupVersion("employees")];
}
