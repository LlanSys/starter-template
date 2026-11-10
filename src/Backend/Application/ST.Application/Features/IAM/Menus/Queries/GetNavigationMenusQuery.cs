using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.Menus.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Menus.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.IAM.Menus.Queries;


public sealed record GetNavigationMenusQuery(string Placement, IReadOnlyList<string> PermissionKeys, string UserId, IReadOnlyList<string>? RoleNames = null)
    : IRequest<AppResponse<IReadOnlyList<MenuResponse>>>, ICachableRequest
{
    public string CacheGroup => "menus";
    public string Discriminator => CacheKeys.Discriminator(new { Placement, Roles = (RoleNames ?? []).Order(StringComparer.OrdinalIgnoreCase), Permissions = PermissionKeys.Order(StringComparer.OrdinalIgnoreCase) });
    public string? CacheUserId => UserId;
    public bool BypassCache => true;
    public bool IsVersioned => true;
}
