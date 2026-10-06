using ST.Application.Contracts.Interfaces.Common;
using ST.SharedKernel.Extensions;
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
using ST.Domain.Features.Banking.Customers.Entities;
using ST.Domain.Features.Banking.Customers.Enums;
using ST.Domain.Features.Banking.Customers.ValueObjects;
using ST.SharedKernel.Features.Banking.Customers.Dtos;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Features.Banking.Customers.CommandHandlers;


/// <summary>
/// Invalidation:
///   - Direct:  delete the entity entry so the next GetById call fetches fresh data.
///   - Version: bump the global "customers" and "dashboard" versions to orphan list entries.
///
/// Both are necessary: without the direct deletion the entity detail page would
/// still show stale data even after the list refreshes.
/// </summary>

public record UpdateCustomerCommand(Guid Id, UpdateCustomerRequest UpdateCustomerRequest, string UserId)
    : IRequest<AppResponse<CustomerResponse>>, ICacheInvalidatorRequest
{
    public IReadOnlyList<string> DirectInvalidationKeys => [CacheKeys.Entity("customers", Id.ToString())];
    public IReadOnlyList<string> GroupVersionKeysToInvalidate =>
    [
        CacheKeys.GroupVersion("customers"),
        CacheKeys.GroupVersion("dashboard")
    ];

}
