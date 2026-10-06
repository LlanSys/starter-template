using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ST.Application.Features.ControlPlane.Stamps.Queries;
using ST.SharedKernel.Dtos.Common;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.SharedKernel.Features.ControlPlane.Stamps.Dtos;
using ST.SharedKernel.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ST.Infrastructure.Features.ControlPlane.Stamps.QueryHandlers;

public class GetDeploymentStampByIdQueryHandler : IRequestHandler<GetDeploymentStampByIdQuery, AppResponse<DeploymentStampResponse>>
{
    private readonly IControlPlaneUnitOfWork _unitOfWork;

    public GetDeploymentStampByIdQueryHandler(IControlPlaneUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AppResponse<DeploymentStampResponse>> Handle(GetDeploymentStampByIdQuery request, CancellationToken cancellationToken)
    {
        var stamp = await _unitOfWork.DeploymentStamps
            .FindAll()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (stamp == null)
        {
            return AppResponses.Failure<DeploymentStampResponse>("Deployment stamp not found.");
        }

        var response = new DeploymentStampResponse
        {
            Id = stamp.Id,
            Name = stamp.Name,
            TargetResourceGroup = stamp.TargetResourceGroup,
            IsolationTier = stamp.IsolationTier.ToDisplayString(),
            KeyVaultUri = stamp.KeyVaultUri,
            DatabaseProvider = stamp.DatabaseProvider,
            DatabaseConnectionString = stamp.DatabaseConnectionString != null ? "********" : null,
            CreatedAt = stamp.CreatedAt,
            UpdatedAt = stamp.UpdatedAt
        };

        return AppResponses.Success(response);
    }
}
