using System.Collections.ObjectModel;
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.Menus.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Menus.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Menus.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.IAM.Menus.Queries;


public sealed record SearchMenusQuery(MenuSearchRequest SearchRequest, string UserId)
    : IRequest<AppResponse<PagedResponse<MenuResponse, Guid>>>, ICachableRequest
{
    public string CacheGroup => "menus";
    public string Discriminator => CacheKeys.Discriminator(SearchRequest);
    public string? CacheUserId => null;
    public bool IsVersioned => true;
}
