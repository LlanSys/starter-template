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

    public ActivateTenantCommandHandler(
        IControlPlaneUnitOfWork unitOfWork,
        ILogger<ActivateTenantCommandHandler> logger,
        ICurrentActorProvider actorProvider,
        TimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _actorProvider = actorProvider;
        _timeProvider = timeProvider;
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
            UpdatedAt = tenant.UpdatedAt
        };
        return AppResponses.Success(dto);
    }
}
