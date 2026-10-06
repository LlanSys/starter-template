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
using ST.SharedKernel.Features.Banking.Customers.Dtos;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Features.Banking.Customers.QueryHandlers;

/// <summary>
/// Fetches a single customer by ID.
///
/// Cache strategy — NON-VERSIONED entity entry:
///   Key:  "customers:entity:tenant:{tenantId}:{id}"  (tenant-scoped)
///         "customers:entity:{id}"                     (platform admins only)
///   TTL:  30 minutes
///   Scope: per-tenant — customer data must never cross tenant boundaries.
///
/// Invalidation:
///   UpdateCustomerCommand and DeleteCustomerCommand must include
///   CacheKeys.Entity("customers", id) in their DirectInvalidationKeys.
///   CacheInvalidationBehavior automatically also removes the tenant-scoped variant.
/// </summary>

public record GetCustomerByIdQuery(Guid Id) : IRequest<AppResponse<CustomerResponse>>, ICachableRequest
{
    public string CacheGroup => "customers";
    public string Discriminator => Id.ToString();
    public string? CacheUserId => null;
    public bool IsVersioned => false;
}
