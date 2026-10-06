using ST.Application.Contracts.Interfaces.Common;
using ST.SharedKernel.Extensions;
using ST.Application.Features.Banking.Customers.Mappings;
using ST.Application.Features.HR.Employees.Mappings;
using ST.Application.Features.IAM.Users.Mappings;
using ST.Application.Features.Shared.EmailTemplates.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.Banking.Customers.Contracts.Specifications;
using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Shared.Contracts.Specifications;
using ST.Domain.Features.Banking.Customers.Entities;
using ST.SharedKernel.Features.Banking.Customers.Dtos;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Text;

namespace ST.Application.Features.Banking.Customers.QueryHandlers;


// ════════════════════════════════════════════════════════════════════════════════
//  GET CUSTOMER LIST  (unfiltered, paginated)
// ════════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Returns the full customer list with cursor-based pagination, no search filters.
///
/// Cache strategy — VERSIONED list entry:
///   Key:  "customers:list:global:{versionToken}:{discriminator}"
///   TTL:  30 minutes
///   Scope: global — the handler currently returns the same data for every user.
///
/// Invalidation: any mutation command bumps CacheKeys.GroupVersion("customers"),
/// which orphans every versioned entry for that user in O(1).
/// </summary>

public record GetCustomerListQuery(CustomerListRequest CustomerListRequest, string UserId)
    : IRequest<AppResponse<PagedResponse<CustomerResponse, Guid>>>, ICachableRequest
{
    public string CacheGroup => "customers";
    public string Discriminator => CacheKeys.Discriminator(new CustomerListRequest(CustomerListRequest.Cursor, CustomerListRequest.PageSize));
    public string? CacheUserId => null;
    public bool IsVersioned => true;
}
