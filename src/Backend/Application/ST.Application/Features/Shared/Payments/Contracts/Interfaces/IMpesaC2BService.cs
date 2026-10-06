using ST.SharedKernel.Dtos.Common;
using System.Threading;
using System.Threading.Tasks;

namespace ST.Application.Features.Shared.Payments.Contracts.Interfaces;

public interface IMpesaC2BService
{
    Task<AppResponse<string>> RegisterUrlsAsync(CancellationToken cancellationToken = default);
    Task<AppResponse<string>> SimulatePaymentAsync(decimal amount, string phoneNumber, string billRefNumber, CancellationToken cancellationToken = default);
}
