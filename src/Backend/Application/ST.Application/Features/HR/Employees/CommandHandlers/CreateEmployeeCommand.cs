using ST.Application.Features.Banking.Customers.Mappings;
using ST.Application.Features.HR.Employees.Mappings;
using ST.Application.Features.IAM.Users.Mappings;
using ST.Application.Features.Shared.EmailTemplates.Mappings;
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.HR.Employees.Entities;
using ST.Application.Features.HR.Employees.Contracts.Interfaces;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.HR.Employees.Dtos;
using ST.SharedKernel.Features.Shared.Phone;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.HR.Employees.CommandHandlers;


public sealed record CreateEmployeeCommand(CreateEmployeeRequest Request, string User) : IRequest<AppResponse<EmployeeResponse>>, ICacheInvalidatorRequest
{
    public IReadOnlyList<string> DirectInvalidationKeys => [];
    public IReadOnlyList<string> GroupVersionKeysToInvalidate => [CacheKeys.GroupVersion("employees")];
}
