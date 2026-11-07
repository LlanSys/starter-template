using ST.Domain.Features.ControlPlane.Auditing.Entities;
using ST.Domain.Features.ControlPlane.Auditing.Enums;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Auditing.Dtos;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using ST.Application.Utilities;

namespace ST.Application.Features.ControlPlane.Auditing.Commands;

public record StartImpersonationCommand(Guid TargetTenantId, string Reason, int DurationHours = 1) : IRequest<AppResponse<ImpersonationRecordResponse>>;


internal sealed class StartImpersonationCommandHandler(
    IControlPlaneUnitOfWork unitOfWork,
    ICurrentActorProvider actorProvider,
    TimeProvider timeProvider,
    ILogger<StartImpersonationCommandHandler> logger)
    : IRequestHandler<StartImpersonationCommand, AppResponse<ImpersonationRecordResponse>>
{
    public async Task<AppResponse<ImpersonationRecordResponse>> Handle(StartImpersonationCommand request, CancellationToken cancellationToken)
    {
        var tenant = await unitOfWork.Tenants.FindByIdAsync(request.TargetTenantId, cancellationToken).ConfigureAwait(false);
        if (tenant == null)
        {
            return AppResponses.Failure<ImpersonationRecordResponse>("Tenant not found");
        }

        var now = timeProvider.GetUtcNow();
        var expiryTime = now.AddHours(request.DurationHours);
        var actorName = actorProvider.ActorId;

        var record = new ImpersonationRecord
        {
            ActorId = actorProvider.ActorId,
            ActorName = actorName,
            TargetTenantId = tenant.Id,
            TargetTenantName = tenant.DisplayName,
            Reason = request.Reason,
            StartTime = now,
            ExpiryTime = expiryTime,
            Status = ImpersonationRecordStatus.Active,
            CreatedBy = actorProvider.ActorId
        };

        await unitOfWork.ImpersonationRecords.CreateAsync(record, cancellationToken).ConfigureAwait(false);
        await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

        LogDefinitions.LogImpersonationStarted(logger, actorProvider.ActorId, request.TargetTenantId, expiryTime, request.Reason);

        return AppResponses.Success(new ImpersonationRecordResponse(
            record.Id,
            record.ActorId,
            record.ActorName,
            record.TargetTenantId,
            record.TargetTenantName,
            record.Reason,
            record.CreatedAt,
            record.ExpiryTime,
            record.Status.ToString()
        ));
    }
}
