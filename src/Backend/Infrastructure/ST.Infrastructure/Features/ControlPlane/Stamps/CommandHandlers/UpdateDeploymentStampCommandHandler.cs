using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Application.Contracts.Interfaces.Common;
using System;
using System.Threading;
using System.Threading.Tasks;
using ST.Application.Features.ControlPlane.Stamps.Commands;
using ST.SharedKernel.Dtos.Common;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.SharedKernel.Features.ControlPlane.Stamps.Dtos;
using ST.SharedKernel.Extensions;
using ST.Infrastructure.Logging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ST.Infrastructure.Features.ControlPlane.Stamps.CommandHandlers;

public class UpdateDeploymentStampCommandHandler : IRequestHandler<UpdateDeploymentStampCommand, AppResponse<DeploymentStampResponse>>
{
    private readonly IControlPlaneUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateDeploymentStampCommandHandler> _logger;
    private readonly IEncryptionService _encryptionService;

    public UpdateDeploymentStampCommandHandler(
        IControlPlaneUnitOfWork unitOfWork,
        ILogger<UpdateDeploymentStampCommandHandler> logger,
        IEncryptionService encryptionService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _encryptionService = encryptionService;
    }

    public async Task<AppResponse<DeploymentStampResponse>> Handle(UpdateDeploymentStampCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        var stamp = await _unitOfWork.DeploymentStamps.FindByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);

        if (stamp == null)
        {
            return AppResponses.Failure<DeploymentStampResponse>("Deployment stamp not found.");
        }

        var duplicateName = await _unitOfWork.DeploymentStamps.AnyAsync(s => s.Name == request.Request.Name && s.Id != request.Id, cancellationToken).ConfigureAwait(false);

        if (duplicateName)
        {
            return AppResponses.Failure<DeploymentStampResponse>("Another deployment stamp with this name already exists.");
        }

        IsolationTier parsedTier;
        try
        {
            parsedTier = request.Request.IsolationTier.ToEnum<IsolationTier>();
        }
        catch (ArgumentException)
        {
            return AppResponses.Failure<DeploymentStampResponse>("Invalid Isolation Tier.");
        }

        stamp.Name = request.Request.Name;
        stamp.TargetResourceGroup = request.Request.TargetResourceGroup;
        stamp.IsolationTier = parsedTier;
        stamp.KeyVaultUri = request.Request.KeyVaultUri;

        if (request.Request.DatabaseProvider != null)
        {
            stamp.DatabaseProvider = request.Request.DatabaseProvider;
        }

        if (request.Request.DatabaseConnectionString != null)
        {
            stamp.DatabaseConnectionString = request.Request.DatabaseConnectionString != ""
                ? _encryptionService.Encrypt(request.Request.DatabaseConnectionString)
                : null;
        }

        stamp.UpdatedAt = DateTimeOffset.UtcNow;
        stamp.UpdatedBy = "System";

        await _unitOfWork.DeploymentStamps.UpdateAsync(stamp, cancellationToken).ConfigureAwait(false)  ;
        await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

        ControlPlaneLogDefinitions.LogDeploymentStampUpdated(_logger, stamp.Id, stamp.Name);

        var dto = new DeploymentStampResponse
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
        return AppResponses.Success(dto);
    }
}
