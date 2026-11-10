using ST.Application.Contracts.Interfaces.Common;
using System;
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
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Application.Features.ControlPlane.Tenants.Contracts;

namespace ST.Infrastructure.Features.ControlPlane.Tenants.CommandHandlers;

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, AppResponse<TenantResponse>>
{
    private readonly IControlPlaneUnitOfWork _unitOfWork;
    private readonly ILogger<CreateTenantCommandHandler> _logger;
    private readonly IEncryptionService _encryptionService;
    private readonly IStampProvisioner _stampProvisioner;
    private readonly ICurrentActorProvider _actorProvider;
    private readonly TimeProvider _timeProvider;

    public CreateTenantCommandHandler(
        IControlPlaneUnitOfWork unitOfWork,
        ILogger<CreateTenantCommandHandler> logger,
        IEncryptionService encryptionService,
        IStampProvisioner stampProvisioner,
        ICurrentActorProvider actorProvider,
        TimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _encryptionService = encryptionService;
        _stampProvisioner = stampProvisioner;
        _actorProvider = actorProvider;
        _timeProvider = timeProvider;
    }

    public async Task<AppResponse<TenantResponse>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        ArgumentNullException.ThrowIfNull(request.Request, nameof(request.Request));

        var req = request.Request;
        var hostName = req.HostName.Trim().TrimEnd('.').ToLowerInvariant();
        var existingTenant = await _unitOfWork.Tenants.AnyAsync(t => t.Identifier == req.Identifier || t.HostName.ToLower() == hostName, cancellationToken).ConfigureAwait(false);

        if (existingTenant)
        {
            return AppResponses.Failure<TenantResponse>("A tenant with this identifier or hostname already exists.");
        }

        var stamp = await _unitOfWork.DeploymentStamps.FirstOrDefaultAsync(s => s.Id == req.DeploymentStampId, cancellationToken).ConfigureAwait(false);

        if (stamp == null)
        {
            return AppResponses.Failure<TenantResponse>("The specified Deployment Stamp does not exist.");
        }

        if (stamp.IsolationTier == IsolationTier.Isolated
            && await _unitOfWork.Tenants.AnyAsync(tenant => tenant.DeploymentStampId == stamp.Id, cancellationToken).ConfigureAwait(false))
        {
            return AppResponses.Failure<TenantResponse>("This isolated deployment stamp already belongs to a workspace.");
        }

        SubscriptionTier parsedTier;
        try
        {
            parsedTier = req.SubscriptionTier.ToEnum<SubscriptionTier>();
        }
        catch (ArgumentException)
        {
            return AppResponses.Failure<TenantResponse>("Invalid Subscription Tier.");
        }

        var now = _timeProvider.GetUtcNow();
        var tenant = new Tenant
        {
            Identifier = req.Identifier,
            DisplayName = req.DisplayName,
            HostName = hostName,
            ContactEmail = req.ContactEmail,
            AdministratorEmail = req.InitialAdministrator?.AdministratorEmail.Trim(),
            CreatedAt = now,
            MaxUsers = req.MaxUsers,
            SubscriptionTier = parsedTier,
            DeploymentStampId = req.DeploymentStampId,
            ExclusiveStampId = stamp.IsolationTier == IsolationTier.Isolated ? stamp.Id : null,
            DatabaseProvider = req.DatabaseProvider,
            DatabaseConnectionString = !string.IsNullOrWhiteSpace(req.DatabaseConnectionString)
                ? _encryptionService.Encrypt(req.DatabaseConnectionString)
                : null,
            CreatedBy = _actorProvider.ActorId,
        };

        await _unitOfWork.Tenants.CreateAsync(tenant, cancellationToken).ConfigureAwait(false);
        if (req.InitialAdministrator is { SendInvitation: true } administrator)
        {
            await _unitOfWork.AdministratorInvitations.CreateAsync(new TenantAdministratorInvitation(tenant.Id,
                administrator.AdministratorEmail, administrator.FirstName, administrator.LastName, _actorProvider.ActorId, now),
                cancellationToken).ConfigureAwait(false);
        }

        await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

        ControlPlaneLogDefinitions.LogTenantCreated(_logger, tenant.Id, tenant.Identifier);

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
