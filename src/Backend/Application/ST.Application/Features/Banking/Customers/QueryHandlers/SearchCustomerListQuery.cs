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
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ST.Application.Features.Banking.Customers.QueryHandlers;


public record SearchCustomerListQuery(CustomerSearchRequest SearchRequest, string UserId)
    : IRequest<AppResponse<PagedResponse<CustomerResponse, Guid>>>, ICachableRequest
{

    public string CacheGroup => "customers";
    public string Discriminator => CacheKeys.Discriminator(new CustomerSearchRequest(
        SearchRequest.GlobalSearch,
        SearchRequest.Type,
        SearchRequest.SegmentType,
        SearchRequest.SubSegmentType,
        SearchRequest.IdentificationType,
        SearchRequest.LineOfBusiness,
        SearchRequest.Status,
        SearchRequest.RelationshipManagerId,
        SearchRequest.Cursor,
        SearchRequest.PageSize));

    public string? CacheUserId => null;
    public bool IsVersioned => true;
    public bool BypassCache => false;  // explicit; see XML doc above
}
