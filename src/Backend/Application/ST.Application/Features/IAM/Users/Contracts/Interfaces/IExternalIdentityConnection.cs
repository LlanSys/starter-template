using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;

namespace ST.Application.Features.IAM.Users.Contracts.Interfaces;

public interface IExternalIdentityConnection
{
    Task<AppResponse<string>> StartAsync(ConnectExternalIdentityRequest request, CancellationToken cancellationToken);
    Task<AppResponse<bool>> ConnectAsync(string ticket, string provider, string providerKey, CancellationToken cancellationToken);
}
