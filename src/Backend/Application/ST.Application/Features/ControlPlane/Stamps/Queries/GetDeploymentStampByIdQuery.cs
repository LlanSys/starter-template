using System;
using ST.Application.Contracts.Interfaces.Common;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Stamps.Dtos;
using MediatR;

namespace ST.Application.Features.ControlPlane.Stamps.Queries;

public record GetDeploymentStampByIdQuery(Guid Id) : IRequest<AppResponse<DeploymentStampResponse>>, ICachableRequest
{
    public string CacheGroup => "stamps";
    public string Discriminator => Id.ToString();
    public string? CacheUserId => null;
    public bool IsVersioned => false;
}
