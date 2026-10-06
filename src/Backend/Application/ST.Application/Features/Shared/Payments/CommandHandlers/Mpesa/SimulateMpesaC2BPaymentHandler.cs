using ST.Application.Features.Shared.Payments.Contracts.Interfaces;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace ST.Application.Features.Shared.Payments.CommandHandlers.Mpesa;

internal sealed class SimulateMpesaC2BPaymentHandler(IMpesaC2BService mpesaC2BService) : IRequestHandler<SimulateMpesaC2BPaymentCommand, AppResponse<string>>
{
    public Task<AppResponse<string>> Handle(SimulateMpesaC2BPaymentCommand request, CancellationToken cancellationToken)
    {
        return mpesaC2BService.SimulatePaymentAsync(request.Amount, request.PhoneNumber, request.BillRefNumber, cancellationToken);
    }
}
