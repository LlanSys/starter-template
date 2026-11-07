using System;
using System.Collections.Generic;
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Stamps.Dtos;
using MediatR;

namespace ST.Application.Features.ControlPlane.Stamps.Commands;

public record UpdateDeploymentStampCommand(Guid Id, UpdateDeploymentStampRequest Request) : IRequest<AppResponse<DeploymentStampResponse>>, ICacheInvalidatorRequest
{
    public IReadOnlyList<string> DirectInvalidationKeys => [CacheKeys.Entity("stamps", Id.ToString("D"))];

    public IReadOnlyList<string> GroupVersionKeysToInvalidate => [CacheKeys.GroupVersion("stamps")];
}
