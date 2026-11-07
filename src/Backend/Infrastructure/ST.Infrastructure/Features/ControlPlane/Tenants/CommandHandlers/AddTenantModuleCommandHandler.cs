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

namespace ST.Infrastructure.Features.ControlPlane.Tenants.CommandHandlers;

public class AddTenantModuleCommandHandler : IRequestHandler<AddTenantModuleCommand, AppResponse<TenantResponse>>
{
    private readonly IControlPlaneUnitOfWork _unitOfWork;
    private readonly ILogger<AddTenantModuleCommandHandler> _logger;
    private readonly ICurrentActorProvider _actorProvider;

    public AddTenantModuleCommandHandler(
        IControlPlaneUnitOfWork unitOfWork,
        ILogger<AddTenantModuleCommandHandler> logger,
        ICurrentActorProvider actorProvider)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _actorProvider = actorProvider;
    }

    public async Task<AppResponse<TenantResponse>> Handle(AddTenantModuleCommand request, CancellationToken cancellationToken)
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
        var definition = await _unitOfWork.Modules.FirstOrDefaultAsync(module => module.Key == moduleKey && module.IsActive && !module.IsRetired, cancellationToken).ConfigureAwait(false);
        if (definition is null || !(moduleKey.Equals("Core", StringComparison.OrdinalIgnoreCase) || moduleKey.Equals("IAM", StringComparison.OrdinalIgnoreCase) || moduleKey.Equals("Showcase", StringComparison.OrdinalIgnoreCase) || moduleKey.Equals("Payments", StringComparison.OrdinalIgnoreCase)))
            return AppResponses.Failure<TenantResponse>("Select an active, implemented module from the catalogue.");
        moduleKey = definition.Key;
        var existingModule = tenant.Modules.FirstOrDefault(m => string.Equals(m.ModuleKey, moduleKey, StringComparison.OrdinalIgnoreCase));

        if (existingModule != null)
        {
            if (!existingModule.IsActive)
            {
                existingModule.IsActive = true;
                existingModule.ExpiresAt = request.Request.ExpiresAt;
                existingModule.UpdatedAt = DateTimeOffset.UtcNow;
                existingModule.UpdatedBy = _actorProvider.ActorId;
            }
            else
            {
                return AppResponses.Failure<TenantResponse>($"Module '{moduleKey}' is already active for this tenant.");
            }
        }
        else
        {
            await _unitOfWork.TenantModules.CreateAsync(new TenantModule
            {
                TenantId = tenant.Id,
                ModuleKey = moduleKey,
                IsActive = true,
                ExpiresAt = request.Request.ExpiresAt,
                CreatedBy = _actorProvider.ActorId
            }, cancellationToken).ConfigureAwait(false);
        }

        await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

        ControlPlaneLogDefinitions.LogTenantModuleAdded(_logger, moduleKey, tenant.Id);

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
