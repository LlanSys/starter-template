using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ST.Application.Features.ControlPlane.Tenants.Commands;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using ST.SharedKernel.Extensions;
using ST.Infrastructure.Logging;
using ST.Domain.Shared.Contracts.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ST.Domain.Features.ControlPlane.Tenants.Events;

namespace ST.Infrastructure.Features.ControlPlane.Tenants.CommandHandlers;

public class RemoveTenantModuleCommandHandler : IRequestHandler<RemoveTenantModuleCommand, AppResponse<TenantResponse>>
{
    private readonly TimeProvider _timeProvider;
    private readonly IControlPlaneUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveTenantModuleCommandHandler> _logger;
    private readonly ICurrentActorProvider _actorProvider;

    public RemoveTenantModuleCommandHandler(
        IControlPlaneUnitOfWork unitOfWork,
        ILogger<RemoveTenantModuleCommandHandler> logger,
        ICurrentActorProvider actorProvider, TimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _timeProvider = timeProvider;
        _actorProvider = actorProvider;
    }

    public async Task<AppResponse<TenantResponse>> Handle(RemoveTenantModuleCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _unitOfWork.Tenants.FindAll().AsTracking()
            .Include(t => t.Modules)
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (tenant == null)
        {
            return AppResponses.Failure<TenantResponse>("Tenant not found.");
        }

        var moduleKey = request.Request.ModuleKey.Trim();
        if (moduleKey.Equals("Core", StringComparison.OrdinalIgnoreCase) || moduleKey.Equals("IAM", StringComparison.OrdinalIgnoreCase)) return AppResponses.Failure<TenantResponse>("Core workspace and identity capabilities are required for every tenant.");
        var existingModule = tenant.Modules.FirstOrDefault(m => string.Equals(m.ModuleKey, moduleKey, StringComparison.OrdinalIgnoreCase));

        if (existingModule != null && existingModule.IsActive)
        {
            var now = _timeProvider.GetUtcNow();
            existingModule.IsActive = false;
            existingModule.UpdatedAt = now;
            existingModule.UpdatedBy = _actorProvider.ActorId;

            tenant.RaiseDomainEvent(new TenantModuleRevokedDomainEvent(tenant.Id, moduleKey, now));

            await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
            ControlPlaneLogDefinitions.LogTenantModuleRemoved(_logger, moduleKey, tenant.Id);
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
            EnabledModules = tenant.Modules.Where(m => m.IsActive).Select(m => m.ModuleKey).ToList(),
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt
        };

        return AppResponses.Success(dto);
    }
}
