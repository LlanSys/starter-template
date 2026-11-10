using MediatR;
using Microsoft.Extensions.Logging;
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.ControlPlane.Tenants.Contracts;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.SharedKernel.Dtos.Common;

namespace ST.Application.Features.ControlPlane.Tenants.Commands;

public record CompleteTenantProvisioningCommand(
    Guid TenantId,
    string DatabaseConnectionString,
    string ApplicationInsightsKey,
    Guid? OperationId = null,
    bool Succeeded = true,
    string? DatabaseProvider = null,
    string? ClientOrigin = null) : IRequest<AppResponse<bool>>;

internal sealed partial class CompleteTenantProvisioningCommandHandler(
    IControlPlaneUnitOfWork unitOfWork,
    IEncryptionService encryption,
    ITenantDatabaseReadiness readiness,
    TimeProvider clock,
    ILogger<CompleteTenantProvisioningCommandHandler> logger)
    : IRequestHandler<CompleteTenantProvisioningCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(
        CompleteTenantProvisioningCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await unitOfWork.Tenants.FindByIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        if (tenant is null || !tenant.CanProvision || request.OperationId is null
            || tenant.ProvisioningOperationId != request.OperationId)
        {
            return AppResponses.Failure<bool>("This provisioning attempt is unavailable.");
        }

        if (!request.Succeeded)
        {
            if (tenant.Status == TenantStatus.Provisioning)
            {
                tenant.MarkAsProvisioningFailed();
                await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
            }

            return AppResponses.Success(true);
        }

        if (!WorkspaceClientOrigin.TryNormalize(request.ClientOrigin, out var clientOrigin))
        {
            return AppResponses.Failure<bool>("The workspace client origin is invalid.");
        }

        var stamp = await unitOfWork.DeploymentStamps.FindByIdAsync(
            tenant.DeploymentStampId, cancellationToken).ConfigureAwait(false);
        var provider = tenant.DatabaseProvider ?? stamp?.DatabaseProvider;
        if (stamp?.IsolationTier != IsolationTier.Isolated || string.IsNullOrWhiteSpace(provider)
            || string.IsNullOrWhiteSpace(request.DatabaseConnectionString)
            || (request.DatabaseProvider is not null
                && !string.Equals(provider, request.DatabaseProvider, StringComparison.OrdinalIgnoreCase)))
        {
            return AppResponses.Failure<bool>("The provisioning result does not match this workspace's deployment stamp.");
        }

        if (tenant.Status == TenantStatus.Active)
        {
            return tenant.DatabaseConnectionString is not null
                && string.Equals(encryption.Decrypt(tenant.DatabaseConnectionString), request.DatabaseConnectionString, StringComparison.Ordinal)
                && string.Equals(tenant.ClientOrigin, clientOrigin, StringComparison.OrdinalIgnoreCase)
                ? AppResponses.Success(true)
                : AppResponses.Failure<bool>("This provisioning attempt has already completed with a different destination.");
        }

        if (tenant.Status != TenantStatus.Provisioning || tenant.IsProvisioningExpired(clock.GetUtcNow()))
        {
            return AppResponses.Failure<bool>("This provisioning attempt is no longer active.");
        }

        var verified = await readiness.VerifyAsync(
            tenant.Id, request.DatabaseConnectionString, provider, cancellationToken).ConfigureAwait(false);
        if (!verified.IsSuccess)
        {
            return verified;
        }

        tenant.DatabaseConnectionString = encryption.Encrypt(request.DatabaseConnectionString);
        tenant.DatabaseProvider = provider;
        tenant.ClientOrigin = clientOrigin;
        tenant.MarkAsActive();
        await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
        LogCompleted(logger, tenant.Id, request.OperationId.Value);
        return AppResponses.Success(true);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Completed workspace {TenantId} provisioning attempt {OperationId}.")]
    private static partial void LogCompleted(ILogger logger, Guid tenantId, Guid operationId);
}
