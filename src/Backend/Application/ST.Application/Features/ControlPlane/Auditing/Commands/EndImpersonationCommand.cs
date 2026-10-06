using ST.Domain.Features.ControlPlane.Auditing.Enums;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using ST.Application.Utilities;

namespace ST.Application.Features.ControlPlane.Auditing.Commands;

public record EndImpersonationCommand(Guid ImpersonationRecordId) : IRequest<AppResponse<bool>>;

internal sealed class EndImpersonationCommandHandler(
    IControlPlaneUnitOfWork unitOfWork,
    ICurrentActorProvider actorProvider,
    TimeProvider timeProvider,
    ILogger<EndImpersonationCommandHandler> logger)
    : IRequestHandler<EndImpersonationCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(EndImpersonationCommand request, CancellationToken cancellationToken)
    {
        var record = await unitOfWork.ImpersonationRecords.FindByIdAsync(request.ImpersonationRecordId, cancellationToken).ConfigureAwait(false);

        if (record == null)
        {
            return AppResponses.Failure<bool>("Impersonation record not found");
        }

        if (record.ActorId != actorProvider.ActorId)
        {
            return AppResponses.Failure<bool>("You cannot end an impersonation session belonging to another user");
        }

        if (record.Status != ImpersonationRecordStatus.Active)
        {
            return AppResponses.Success(true); // Already ended/expired
        }

        record.Status = ImpersonationRecordStatus.Exited;
        record.ExpiryTime = timeProvider.GetUtcNow(); // Explicitly cap it

        await unitOfWork.ImpersonationRecords.UpdateAsync(record, cancellationToken).ConfigureAwait(false);
        await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

        LogDefinitions.LogImpersonationEnded(logger, actorProvider.ActorId, record.Id);

        return AppResponses.Success(true);
    }
}
