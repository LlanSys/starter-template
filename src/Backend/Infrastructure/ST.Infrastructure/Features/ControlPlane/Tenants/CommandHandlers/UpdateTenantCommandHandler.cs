using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Application.Contracts.Interfaces.Common;
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

public class UpdateTenantCommandHandler : IRequestHandler<UpdateTenantCommand, AppResponse<TenantResponse>>
{
    private readonly TimeProvider _timeProvider;
    private readonly IControlPlaneUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateTenantCommandHandler> _logger;
    private readonly IEncryptionService _encryptionService;
    private readonly ICurrentActorProvider _actorProvider;

    public UpdateTenantCommandHandler(
        IControlPlaneUnitOfWork unitOfWork,
        ILogger<UpdateTenantCommandHandler> logger,
        IEncryptionService encryptionService,
        ICurrentActorProvider actorProvider, TimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _timeProvider = timeProvider;
        _encryptionService = encryptionService;
        _actorProvider = actorProvider;
    }

    public async Task<AppResponse<TenantResponse>> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        var tenant = await _unitOfWork.Tenants.FindByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);

        if (tenant == null)
        {
            return AppResponses.Failure<TenantResponse>("Tenant not found.");
        }

        var duplicateHostName = await _unitOfWork.Tenants.AnyAsync(t => t.HostName == request.Request.HostName && t.Id != request.Id, cancellationToken).ConfigureAwait(false);

        if (duplicateHostName)
        {
            return AppResponses.Failure<TenantResponse>("Another tenant with this hostname already exists.");
        }

        SubscriptionTier parsedTier;
        try
        {
            parsedTier = request.Request.SubscriptionTier.ToEnum<SubscriptionTier>();
        }
        catch (ArgumentException)
        {
            return AppResponses.Failure<TenantResponse>("Invalid Subscription Tier.");
        }

        tenant.DisplayName = request.Request.DisplayName;
        tenant.HostName = request.Request.HostName;
        tenant.ContactEmail = request.Request.ContactEmail;
        tenant.MaxUsers = request.Request.MaxUsers;
        tenant.SubscriptionTier = parsedTier;

        if (request.Request.DatabaseProvider != null)
        {
            tenant.DatabaseProvider = request.Request.DatabaseProvider;
        }

        if (request.Request.DatabaseConnectionString != null)
        {
            tenant.DatabaseConnectionString = request.Request.DatabaseConnectionString != ""
                ? _encryptionService.Encrypt(request.Request.DatabaseConnectionString)
                : null;
        }

        tenant.UpdatedAt = _timeProvider.GetUtcNow();
        tenant.UpdatedBy = _actorProvider.ActorId;

        await _unitOfWork.Tenants.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

        ControlPlaneLogDefinitions.LogTenantUpdated(_logger, tenant.Id, tenant.Identifier);

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
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt
        };
        return AppResponses.Success(dto);
    }
}
