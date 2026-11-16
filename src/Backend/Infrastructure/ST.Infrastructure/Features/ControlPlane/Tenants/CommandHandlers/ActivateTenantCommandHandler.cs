using ST.Application.Features.ControlPlane.Kyc.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using ST.Application.Features.ControlPlane.Tenants.Commands;
using ST.SharedKernel.Dtos.Common;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using ST.SharedKernel.Extensions;
using ST.Infrastructure.Logging;
using ST.Domain.Shared.Contracts.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ST.Infrastructure.Features.ControlPlane.Tenants.CommandHandlers;

public class ActivateTenantCommandHandler : IRequestHandler<ActivateTenantCommand, AppResponse<TenantResponse>>
{
    private readonly IControlPlaneUnitOfWork _unitOfWork;
    private readonly ILogger<ActivateTenantCommandHandler> _logger;
    private readonly ICurrentActorProvider _actorProvider;
    private readonly TimeProvider _timeProvider;
    private readonly IKycRetentionService _retention;

    public ActivateTenantCommandHandler(
        IControlPlaneUnitOfWork unitOfWork,
        ILogger<ActivateTenantCommandHandler> logger,
        ICurrentActorProvider actorProvider,
        TimeProvider timeProvider,
        IKycRetentionService retention)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _actorProvider = actorProvider;
        _timeProvider = timeProvider;
        _retention = retention;
    }

    public async Task<AppResponse<TenantResponse>> Handle(ActivateTenantCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var tenant = await _unitOfWork.Tenants.FindByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);

        if (tenant == null)
        {
            return AppResponses.Failure<TenantResponse>("Tenant not found.");
        }

        if (tenant.Status != TenantStatus.Suspended || !tenant.CanProvision)
        {
            return AppResponses.Failure<TenantResponse>("Only a suspended tenant can be reactivated. New tenants require approval and provisioning.");
        }

        if (tenant.IsCustomerWorkspace)
        {
            var erased = await _retention.CheckErasureAsync(tenant.Id, cancellationToken).ConfigureAwait(false);

            if (!erased.IsSuccess)
            {
                return AppResponses.Failure<TenantResponse>(erased.Error!);
            }

            if (erased.Data)
            {
                return AppResponses.Failure<TenantResponse>("This KYC case has been erased. A new onboarding review is required.");
            }
        }

        if (await _unitOfWork.KycRetentionCases.AnyAsync(x => x.TenantId == tenant.Id && (x.Outcome != "Open" || x.Phase != "Retained"), cancellationToken).ConfigureAwait(false))
        {
            return AppResponses.Failure<TenantResponse>("The KYC case is closed; resolve it before reactivation.");
        }

        var now = _timeProvider.GetUtcNow();
        tenant.Reactivate(now);
        tenant.UpdatedAt = now;
        tenant.UpdatedBy = _actorProvider.ActorId;

        await _unitOfWork.Tenants.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

        ControlPlaneLogDefinitions.LogTenantActivated(_logger, tenant.Id, tenant.Identifier);

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
            CreatedAt = tenant.CreatedAt,
            KycApprovedAt = tenant.KycApprovedAt,
            UpdatedAt = tenant.UpdatedAt
        };

        return AppResponses.Success(dto);
    }
}
