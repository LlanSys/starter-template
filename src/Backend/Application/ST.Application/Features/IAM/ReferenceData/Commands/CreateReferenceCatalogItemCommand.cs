using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.ReferenceData.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.ReferenceData.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.ReferenceData.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.IAM.ReferenceData.Commands;


public sealed record CreateReferenceCatalogItemCommand(string CatalogType, ReferenceCatalogItemRequest Request, string UserId)
    : IRequest<AppResponse<ReferenceCatalogItemResponse>>, ICacheInvalidatorRequest
{
    public IReadOnlyList<string> GroupVersionKeysToInvalidate => [CacheKeys.GroupVersion("iam-reference-data")];
}
