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
using ST.SharedKernel.Extensions;
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


internal sealed class GetCustomerListQueryHandler(IBankingUnitOfWork _bankingUnitOfWork, ILogger<GetCustomerListQueryHandler> _logger)
    : IRequestHandler<GetCustomerListQuery, AppResponse<PagedResponse<CustomerResponse, Guid>>>
{
    public async Task<AppResponse<PagedResponse<CustomerResponse, Guid>>> Handle(GetCustomerListQuery query, CancellationToken ct)
    {
        try
        {
            var req = query.CustomerListRequest;

            var pageSize = Math.Clamp(req.PageSize, 1, 50);

            var spec = new CustomerSearchSpec(
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                req.Cursor,
                pageSize + 1);

            var totalCount = await _bankingUnitOfWork.CustomerRepository.CountAsync(ct).ConfigureAwait(false);
            var customerEntities = await _bankingUnitOfWork.CustomerRepository.SearchAsync(spec, ct).ConfigureAwait(false);

            var hasNextPage = customerEntities.Count > pageSize;

            if (hasNextPage)
                customerEntities.RemoveAt(customerEntities.Count - 1);

            var items = customerEntities.Select(c => c.ToCustomerResponse()).ToList();
            var nextCursor = hasNextPage ? items[^1].Id : (Guid?)null;

            bool isFirstPage = req.Cursor == null || req.Cursor == Guid.Empty;

            var pagedResult = new PagedResponse<CustomerResponse, Guid>(
                new Collection<CustomerResponse>(items),
                totalCount,
                1,
                pageSize,
                isFirstPage,
                nextCursor ?? Guid.Empty
            );

            return AppResponses.Success(pagedResult);
        }
        catch (Exception ex)
        {
            LogDefinitions.LogCustomerListFetchFailed(_logger, ex);
            throw;
        }
    }
}
