using System;
using ST.Application.Features.ControlPlane.Tenants.Contracts;
using System.Threading;
using System.Threading.Tasks;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Domain.Features.ControlPlane.Tenants.Events;
using ST.Domain.Shared.Contracts.Common;
using ST.SharedKernel.Dtos.Common;
using ST.Application.Contracts.Interfaces.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.ControlPlane.Tenants.Commands;

public record MigrateTenantStampCommand(
    Guid TenantId,
    Guid NewDeploymentStampId,
    string NewDatabaseConnectionString,
    string? NewClientOrigin = null) : IRequest<AppResponse<bool>>;


internal sealed partial class MigrateTenantStampCommandHandler : IRequestHandler<MigrateTenantStampCommand, AppResponse<bool>>
{
    private readonly TimeProvider _timeProvider;
    private readonly ITenantDatabaseReadiness _readiness;
    private readonly IControlPlaneUnitOfWork _unitOfWork;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<MigrateTenantStampCommandHandler> _logger;

    public MigrateTenantStampCommandHandler(
        IControlPlaneUnitOfWork unitOfWork,
        IEncryptionService encryptionService,
        ILogger<MigrateTenantStampCommandHandler> logger, TimeProvider timeProvider,
        ITenantDatabaseReadiness readiness)
    {
        _unitOfWork = unitOfWork;
        _encryptionService = encryptionService;
        _logger = logger;
        _timeProvider = timeProvider;
        _readiness = readiness;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Migrating Tenant {TenantId} to Stamp {StampId}.")]
    private partial void LogMigratingTenantStamp(Guid tenantId, Guid stampId);

    public async Task<AppResponse<bool>> Handle(MigrateTenantStampCommand request, CancellationToken cancellationToken)
    {
        LogMigratingTenantStamp(request.TenantId, request.NewDeploymentStampId);

        var tenant = await _unitOfWork.Tenants.FindByIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        if (tenant == null)
        {
            return AppResponses.Failure<bool>("Tenant not found.");
        }

        if (tenant.Status != TenantStatus.Suspended || !tenant.CanProvision)
        {
            return AppResponses.Failure<bool>("Suspend the approved workspace before moving its database.");
        }

        var stamp = await _unitOfWork.DeploymentStamps.FindByIdAsync(request.NewDeploymentStampId, cancellationToken).ConfigureAwait(false);
        if (stamp is null)
        {
            return AppResponses.Failure<bool>("The destination deployment stamp was not found.");
        }

        if (stamp.IsolationTier == IsolationTier.Isolated
            && await _unitOfWork.Tenants.AnyAsync(
                item => item.DeploymentStampId == stamp.Id && item.Id != tenant.Id, cancellationToken).ConfigureAwait(false))
        {
            return AppResponses.Failure<bool>("The isolated destination already belongs to another workspace.");
        }

        var provider = stamp.DatabaseProvider ?? tenant.DatabaseProvider;
        if (string.IsNullOrWhiteSpace(provider))
        {
            return AppResponses.Failure<bool>("The destination database provider is unavailable.");
        }

        var verified = await _readiness.VerifyAsync(
            tenant.Id, request.NewDatabaseConnectionString, provider, cancellationToken, stamp.IsolationTier == IsolationTier.Isolated).ConfigureAwait(false);
        if (!verified.IsSuccess)
        {
            return verified;
        }

        if (!WorkspaceClientOrigin.TryNormalize(request.NewClientOrigin, out var clientOrigin))
        {
            return AppResponses.Failure<bool>("The destination client origin is invalid.");
        }

        var oldStampId = tenant.DeploymentStampId;
        tenant.DeploymentStampId = stamp.Id;
        tenant.ExclusiveStampId = stamp.IsolationTier == IsolationTier.Isolated ? stamp.Id : null;
        tenant.DatabaseProvider = stamp.DatabaseProvider;
        tenant.DatabaseConnectionString = _encryptionService.Encrypt(request.NewDatabaseConnectionString);
        if (clientOrigin is not null)
        {
            tenant.ClientOrigin = clientOrigin;
        }

        // Raise domain event so that sessions/caches for this tenant can be invalidated
        tenant.RaiseDomainEvent(new TenantStampChangedDomainEvent(tenant.Id, oldStampId, request.NewDeploymentStampId, _timeProvider.GetUtcNow()));

        await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

        return AppResponses.Success<bool>("Tenant stamp migration completed successfully.", true);
    }
}
