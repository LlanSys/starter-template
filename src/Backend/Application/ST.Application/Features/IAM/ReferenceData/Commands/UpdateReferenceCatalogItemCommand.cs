using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.ReferenceData.Mappings;
using ST.Application.Utilities;
using ST.SharedKernel.Dtos.Common;
using ST.Domain.Features.IAM.Contracts;
using ST.SharedKernel.Features.IAM.ReferenceData.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.IAM.ReferenceData.Commands;


public sealed record UpdateReferenceCatalogItemCommand(string CatalogType, Guid Id, ReferenceCatalogItemRequest Request, string UserId)
    : IRequest<AppResponse<ReferenceCatalogItemResponse>>, ICacheInvalidatorRequest
{
    public IReadOnlyList<string> GroupVersionKeysToInvalidate => [CacheKeys.GroupVersion("iam-reference-data")];
}
