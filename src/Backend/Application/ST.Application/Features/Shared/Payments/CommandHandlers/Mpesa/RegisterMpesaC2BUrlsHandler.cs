using ST.Application.Features.Shared.Payments.Contracts.Interfaces;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace ST.Application.Features.Shared.Payments.CommandHandlers.Mpesa;

internal sealed class RegisterMpesaC2BUrlsHandler(IMpesaC2BService mpesaC2BService) : IRequestHandler<RegisterMpesaC2BUrlsCommand, AppResponse<string>>
{
    public Task<AppResponse<string>> Handle(RegisterMpesaC2BUrlsCommand request, CancellationToken cancellationToken)
    {
        return mpesaC2BService.RegisterUrlsAsync(cancellationToken);
    }
}
