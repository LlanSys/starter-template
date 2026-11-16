using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ST.Application.Features.ControlPlane.Kyc.Commands;
using ST.Application.Features.ControlPlane.Kyc.Contracts;
using ST.SharedKernel.Dtos.Common;
namespace ST.Infrastructure.Features.ControlPlane.Kyc;

internal sealed partial class ActOnKycRetentionCommandHandler(IKycRetentionService retention, ILogger<ActOnKycRetentionCommandHandler> logger)
    : IRequestHandler<ActOnKycRetentionCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(ActOnKycRetentionCommand request, CancellationToken cancellationToken)
    {
        try { return await retention.ActAsync(request.TenantId, request.Request, cancellationToken).ConfigureAwait(false); }
        catch (DbUpdateConcurrencyException exception)
        {
            LogChanged(logger, exception);
            return AppResponses.Failure<bool>("This record changed. Refresh before continuing.");
        }
    }
    [LoggerMessage(Level = LogLevel.Information, Message = "A KYC retention mutation conflicted with another operation.")]
    private static partial void LogChanged(ILogger logger, Exception exception);
}
