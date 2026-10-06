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
using ST.Domain.Features.Banking.Customers.Enums;
using ST.SharedKernel.Features.Banking.Customers.Dtos;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Extensions;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ST.Application.Features.Banking.Customers.QueryHandlers;



internal sealed class SearchCustomerListQueryHandler(IBankingUnitOfWork _bankingUnitOfWork, ILogger<SearchCustomerListQueryHandler> _logger)
    : IRequestHandler<SearchCustomerListQuery, AppResponse<PagedResponse<CustomerResponse, Guid>>>
{
    public async Task<AppResponse<PagedResponse<CustomerResponse, Guid>>> Handle(SearchCustomerListQuery query, CancellationToken ct)
    {
        try
        {
            var req = query.SearchRequest;

            var pageSize = Math.Clamp(req.PageSize, 1, 50);

            var customerType = req.Type?.ToEnum<CustomerType>();
            var segmentType = req.SegmentType?.ToEnum<SegmentType>();
            var subSegmentType = req.SubSegmentType?.ToEnum<SubSegmentType>();
            var identificationType = req.IdentificationType?.ToEnum<IdentificationType>();
            var lineOfBusiness = req.LineOfBusiness?.ToEnum<LineOfBusiness>();
            var status = req.Status?.ToEnum<CustomerStatus>();

            var spec = new CustomerSearchSpec(
                req.GlobalSearch,
                customerType,
                segmentType,
                subSegmentType,
                identificationType,
                lineOfBusiness,
                status,
                req.RelationshipManagerId,
                req.Cursor,
                pageSize + 1

            );

            var totalCount = await _bankingUnitOfWork.CustomerRepository.CountAsync(spec, ct).ConfigureAwait(false);
            var customerEntities = await _bankingUnitOfWork.CustomerRepository.SearchAsync(spec, ct).ConfigureAwait(false);

            bool hasNextPage = customerEntities.Count > pageSize;
            if (hasNextPage)
                customerEntities.RemoveAt(customerEntities.Count - 1);

            var items = customerEntities.Select(x => x.ToCustomerResponse()).ToList();

            var nextCursor = hasNextPage ? items[^1].Id : (Guid?)null;

            // Display sort — purely cosmetic, does not affect pagination
            items = [.. items.OrderBy(x => x.Number, StringComparer.OrdinalIgnoreCase)];

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
