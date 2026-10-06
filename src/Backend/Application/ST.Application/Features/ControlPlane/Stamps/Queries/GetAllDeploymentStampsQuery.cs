using System.Collections.Generic;
using ST.Application.Contracts.Interfaces.Common;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Stamps.Dtos;
using MediatR;

namespace ST.Application.Features.ControlPlane.Stamps.Queries;

public record GetAllDeploymentStampsQuery : IRequest<AppResponse<List<DeploymentStampResponse>>>, ICachableRequest
{
    public string CacheGroup => "stamps";
    public string Discriminator => "all";
    public string? CacheUserId => null;
    public bool IsVersioned => true;
}
