using System;
using System.Threading;
using System.Threading.Tasks;
using ST.Domain.Features.ControlPlane.Contracts;
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
    string NewDatabaseConnectionString) : IRequest<AppResponse<bool>>;


internal sealed partial class MigrateTenantStampCommandHandler : IRequestHandler<MigrateTenantStampCommand, AppResponse<bool>>
{
    private readonly IControlPlaneUnitOfWork _unitOfWork;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<MigrateTenantStampCommandHandler> _logger;

    public MigrateTenantStampCommandHandler(
        IControlPlaneUnitOfWork unitOfWork,
        IEncryptionService encryptionService,
        ILogger<MigrateTenantStampCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _encryptionService = encryptionService;
        _logger = logger;
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

        var oldStampId = tenant.DeploymentStampId;

        tenant.DeploymentStampId = request.NewDeploymentStampId;
        tenant.DatabaseConnectionString = _encryptionService.Encrypt(request.NewDatabaseConnectionString);

        // Raise domain event so that sessions/caches for this tenant can be invalidated
        tenant.RaiseDomainEvent(new TenantStampChangedDomainEvent(tenant.Id, oldStampId, request.NewDeploymentStampId));

        await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

        return AppResponses.Success<bool>("Tenant stamp migration completed successfully.", true);
    }
}
