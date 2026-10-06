using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.Banking.Customers.Contracts.Interfaces;
using ST.Application.Features.Banking.Customers.IntegrationEvents;
using ST.Application.Features.HR.Employees.IntegrationEvents;
using ST.Application.Features.IAM.Users.IntegrationEvents;
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
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace ST.Application.Features.Banking.Customers.CommandHandlers;

/// <summary>
/// Invalidation: bump the global version tokens for customer lists and dashboards.
/// No entity key to delete (the entity does not exist in cache yet).
/// Versioned list entries are orphaned in O(1).
/// </summary>

public sealed record CreateCustomerCommand(CreateCustomerRequest CreateCustomerRequest, string UserId)
    : IRequest<AppResponse<CustomerResponse>>, ICacheInvalidatorRequest
{
    // No direct keys — new entity, nothing cached yet.
    public IReadOnlyList<string> DirectInvalidationKeys => [];

    public IReadOnlyList<string> GroupVersionKeysToInvalidate =>
    [
        CacheKeys.GroupVersion("customers"),
        CacheKeys.GroupVersion("dashboard")
    ];


}
