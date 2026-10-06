using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.Menus.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Menus.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.IAM.Menus.Queries;


public sealed record GetMenuByIdQuery(Guid Id, string UserId) : IRequest<AppResponse<MenuResponse>>, ICachableRequest
{
    public string CacheGroup => "menus";
    public string Discriminator => CacheKeys.Entity("menus", Id.ToString());
    public string? CacheUserId => null;
    public bool IsVersioned => true;
}
