using System;
using System.Threading;
using System.Threading.Tasks;
using ST.Application.Features.ControlPlane.Tenants.Commands;
using ST.Application.Features.ControlPlane.Tenants.Contracts;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Domain.Shared.Contracts.Common;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using ST.SharedKernel.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ST.Infrastructure.Features.ControlPlane.Tenants.CommandHandlers;

public class ApproveTenantCommandHandler : IRequestHandler<ApproveTenantCommand, AppResponse<TenantResponse>>
{
    private readonly IControlPlaneUnitOfWork _unitOfWork;
    private readonly ILogger<ApproveTenantCommandHandler> _logger;
    private readonly IStampProvisioner _stampProvisioner;
    private readonly ICurrentActorProvider _actorProvider;
    private readonly TimeProvider _timeProvider;

    public ApproveTenantCommandHandler(
        IControlPlaneUnitOfWork unitOfWork,
        ILogger<ApproveTenantCommandHandler> logger,
        IStampProvisioner stampProvisioner,
        ICurrentActorProvider actorProvider,
        TimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _stampProvisioner = stampProvisioner;
        _actorProvider = actorProvider;
        _timeProvider = timeProvider;
    }

    public async Task<AppResponse<TenantResponse>> Handle(ApproveTenantCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var tenant = await _unitOfWork.Tenants.FindByIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        if (tenant == null)
        {
            return AppResponses.Failure<TenantResponse>("Tenant not found.");
        }

        if (!tenant.IsCustomerWorkspace)
        {
            return AppResponses.Failure<TenantResponse>("KYC applies to customer workspaces.");
        }

        var now = _timeProvider.GetUtcNow();
        if (request.Retry
            ? !tenant.CanProvision || (tenant.Status != TenantStatus.ProvisioningFailed && !tenant.IsProvisioningExpired(now))
            : tenant.KycApprovedAt.HasValue)
        {
            return AppResponses.Failure<TenantResponse>("The workspace is unavailable for this transition.");
        }

        var needsProvisioning = request.Retry || tenant.Status == TenantStatus.PendingApproval;

        var stamp = await _unitOfWork.DeploymentStamps.FirstOrDefaultAsync(s => s.Id == tenant.DeploymentStampId, cancellationToken).ConfigureAwait(false);
        if (stamp == null)
        {
            return AppResponses.Failure<TenantResponse>("Deployment stamp for this tenant could not be found.");
        }

        if (stamp.IsolationTier == IsolationTier.Isolated
            && await _unitOfWork.Tenants.AnyAsync(
                item => item.DeploymentStampId == stamp.Id && item.Id != tenant.Id, cancellationToken).ConfigureAwait(false))
        {
            return AppResponses.Failure<TenantResponse>("This isolated deployment stamp already belongs to another workspace.");
        }

        tenant.ExclusiveStampId = stamp.IsolationTier == IsolationTier.Isolated ? stamp.Id : null;
        if (request.Retry)
        {
            tenant.RetryProvisioning(now);
        }
        else
        {
            tenant.Approve(_actorProvider.ActorId, _timeProvider.GetUtcNow(), request.EvidenceReference);
        }

        if (needsProvisioning && stamp.IsolationTier == IsolationTier.Isolated)
        {
            var operationId = tenant.BeginProvisioning(now, TimeSpan.FromHours(2));
            await _unitOfWork.Tenants.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await _stampProvisioner.ProvisionIsolatedStampAsync(
                    tenant.Id.ToString(),
                    stamp.Id.ToString(),
                    stamp.TargetResourceGroup,
                    tenant.DatabaseProvider ?? stamp.DatabaseProvider ?? "PostgreSql",
                    operationId,
                    cancellationToken).ConfigureAwait(false);

                ControlPlaneLogDefinitions.LogStampProvisioningDispatched(_logger, tenant.Id, stamp.Name);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                ControlPlaneLogDefinitions.LogStampProvisioningFailed(_logger, tenant.Id, ex);

                tenant.MarkAsProvisioningFailed();
                try
                {
                    await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (DbUpdateConcurrencyException)
                {
                    // An authenticated completion may have won the race with the failed dispatch response.
                    ControlPlaneLogDefinitions.LogStampProvisioningFailed(_logger, tenant.Id, ex);
                }
            }
        }
        else if (needsProvisioning)
        {
            // A pooled workspace uses the existing infrastructure.
            tenant.MarkAsActive();
        }

        if (!needsProvisioning || stamp.IsolationTier != IsolationTier.Isolated)
        {
            await _unitOfWork.Tenants.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
        }

        var dto = new TenantResponse
        {
            Id = tenant.Id,
            Identifier = tenant.Identifier,
            DisplayName = tenant.DisplayName,
            HostName = tenant.HostName,
            ContactEmail = tenant.ContactEmail,
            MaxUsers = tenant.MaxUsers,
            SubscriptionTier = tenant.SubscriptionTier.ToDisplayString(),
            Status = tenant.Status.ToDisplayString(),
            DeploymentStampId = tenant.DeploymentStampId,
            DatabaseProvider = tenant.DatabaseProvider,
            DatabaseConnectionString = tenant.DatabaseConnectionString != null ? "********" : null,
            KycApprovedAt = tenant.KycApprovedAt,
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt
        };

        return AppResponses.Success(dto);
    }
}
