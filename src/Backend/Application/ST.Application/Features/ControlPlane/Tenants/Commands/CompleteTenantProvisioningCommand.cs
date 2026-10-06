using System;
using System.Threading;
using System.Threading.Tasks;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Domain.Shared.Contracts.Common;
using ST.SharedKernel.Dtos.Common;
using ST.Application.Contracts.Interfaces.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.ControlPlane.Tenants.Commands;

public record CompleteTenantProvisioningCommand(
    Guid TenantId,
    string DatabaseConnectionString,
    string ApplicationInsightsKey) : IRequest<AppResponse<bool>>;


internal sealed partial class CompleteTenantProvisioningCommandHandler : IRequestHandler<CompleteTenantProvisioningCommand, AppResponse<bool>>
{
    private readonly IControlPlaneUnitOfWork _unitOfWork;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<CompleteTenantProvisioningCommandHandler> _logger;

    public CompleteTenantProvisioningCommandHandler(
        IControlPlaneUnitOfWork unitOfWork,
        IEncryptionService encryptionService,
        ILogger<CompleteTenantProvisioningCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Completing provisioning for Tenant {TenantId}.")]
    private partial void LogCompletingProvisioning(Guid tenantId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tenant {TenantId} was not found or is not in Provisioning status.")]
    private partial void LogTenantNotFoundOrNotProvisioning(Guid tenantId);

    public async Task<AppResponse<bool>> Handle(CompleteTenantProvisioningCommand request, CancellationToken cancellationToken)
    {
        LogCompletingProvisioning(request.TenantId);

        var tenant = await _unitOfWork.Tenants.FindByIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        if (tenant == null || tenant.Status != TenantStatus.Provisioning)
        {
            LogTenantNotFoundOrNotProvisioning(request.TenantId);
            return AppResponses.Failure<bool>("Tenant not found or not in provisioning state.");
        }

        tenant.DatabaseConnectionString = _encryptionService.Encrypt(request.DatabaseConnectionString);
        tenant.Status = TenantStatus.Active;

        await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

        return AppResponses.Success<bool>("Provisioning completed successfully.", true);
    }
}
