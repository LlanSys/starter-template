using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.ReferenceData.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.IAM.ReferenceData.Queries;


public sealed record GetIamReferenceDataQuery : IRequest<AppResponse<IamReferenceDataResponse>>, ICachableRequest
{
    public string CacheGroup => "iam-reference-data";
    public string Discriminator => CacheKeys.Discriminator("all");
    public string? CacheUserId => null;
    public bool IsVersioned => true;
}
