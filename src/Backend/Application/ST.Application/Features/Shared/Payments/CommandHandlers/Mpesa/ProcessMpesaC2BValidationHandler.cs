using ST.SharedKernel.Dtos.Common;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace ST.Application.Features.Shared.Payments.CommandHandlers.Mpesa;

internal sealed class ProcessMpesaC2BValidationHandler : IRequestHandler<ProcessMpesaC2BValidationCommand, AppResponse<string>>
{
    public Task<AppResponse<string>> Handle(ProcessMpesaC2BValidationCommand request, CancellationToken cancellationToken)
    {
        // Validation logic - return Success to Accept, Failure to Reject
        return Task.FromResult(AppResponses.Success("Validation successful."));
    }
}
