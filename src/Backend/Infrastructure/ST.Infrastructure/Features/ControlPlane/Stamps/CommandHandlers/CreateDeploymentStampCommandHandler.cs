using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Application.Contracts.Interfaces.Common;
using System;
using System.Threading;
using System.Threading.Tasks;
using ST.Application.Features.ControlPlane.Stamps.Commands;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.SharedKernel.Features.ControlPlane.Stamps.Dtos;
using ST.SharedKernel.Extensions;
using ST.Infrastructure.Logging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ST.Infrastructure.Features.ControlPlane.Stamps.CommandHandlers;

public class CreateDeploymentStampCommandHandler : IRequestHandler<CreateDeploymentStampCommand, AppResponse<DeploymentStampResponse>>
{
    private readonly IControlPlaneUnitOfWork _unitOfWork;
    private readonly ILogger<CreateDeploymentStampCommandHandler> _logger;
    private readonly IEncryptionService _encryptionService;

    public CreateDeploymentStampCommandHandler(
        IControlPlaneUnitOfWork unitOfWork,
        ILogger<CreateDeploymentStampCommandHandler> logger,
        IEncryptionService encryptionService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _encryptionService = encryptionService;
    }

    public async Task<AppResponse<DeploymentStampResponse>> Handle(CreateDeploymentStampCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        var req = request.Request;
        var existingStamp = await _unitOfWork.DeploymentStamps.AnyAsync(s => s.Name == req.Name, cancellationToken).ConfigureAwait(false);

        if (existingStamp)
        {
            return AppResponses.Failure<DeploymentStampResponse>("A deployment stamp with this name already exists.");
        }

        IsolationTier parsedTier;
        try
        {
            parsedTier = req.IsolationTier.ToEnum<IsolationTier>();
        }
        catch (ArgumentException)
        {
            return AppResponses.Failure<DeploymentStampResponse>("Invalid Isolation Tier.");
        }

        var stamp = new DeploymentStamp
        {
            Name = req.Name,
            TargetResourceGroup = req.TargetResourceGroup,
            IsolationTier = parsedTier,
            KeyVaultUri = req.KeyVaultUri,
            DatabaseProvider = req.DatabaseProvider,
            DatabaseConnectionString = !string.IsNullOrWhiteSpace(req.DatabaseConnectionString)
                ? _encryptionService.Encrypt(req.DatabaseConnectionString)
                : null,
            CreatedBy = "System"
        };

        await _unitOfWork.DeploymentStamps.CreateAsync(stamp, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

        ControlPlaneLogDefinitions.LogDeploymentStampCreated(_logger, stamp.Id, stamp.Name);

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
