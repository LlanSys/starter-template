using ST.Application.Features.IAM.Users.Commands;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class ExchangeSsoCode(IDistributedCache cache) : IRequestHandler<ExchangeSsoCodeCommand, AppResponse<LoginResponse>>
{
    public async Task<AppResponse<LoginResponse>> Handle(ExchangeSsoCodeCommand command, CancellationToken cancellationToken)
    {
        var cacheKey = $"SSO_Exchange_{command.Code}";
        var loginResponseJson = await cache.GetStringAsync(cacheKey, cancellationToken).ConfigureAwait(false);
        var loginResponse = loginResponseJson != null ? JsonSerializer.Deserialize<LoginResponse>(loginResponseJson) : null;

        if (loginResponse == null)
        {
            return AppResponses.Failure<LoginResponse>("Invalid or expired exchange code.");
        }

        await cache.RemoveAsync(cacheKey, cancellationToken).ConfigureAwait(false);

        return AppResponses.Success("Code exchanged successfully", loginResponse);
    }
}
