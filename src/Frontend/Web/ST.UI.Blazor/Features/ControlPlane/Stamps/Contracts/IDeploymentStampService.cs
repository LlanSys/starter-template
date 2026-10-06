using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Stamps.Dtos;

namespace ST.UI.Blazor.Features.ControlPlane.Stamps.Contracts;

public interface IDeploymentStampService
{
    Task<AppResponse<List<DeploymentStampResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AppResponse<DeploymentStampResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AppResponse<DeploymentStampResponse>> CreateAsync(CreateDeploymentStampRequest request, CancellationToken cancellationToken = default);
    Task<AppResponse<DeploymentStampResponse>> UpdateAsync(Guid id, UpdateDeploymentStampRequest request, CancellationToken cancellationToken = default);
}
