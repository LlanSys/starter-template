using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Users.Entities;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class RequestPasskeyLoginOptions(
    IPasskeyService passkeyService,
    UserManager<AppUser> userManager,
    IDistributedCache cacheService) : IRequestHandler<RequestPasskeyLoginOptionsCommand, AppResponse<ST.SharedKernel.Features.IAM.Users.Dtos.PasskeyLoginOptionsResponse>>
{
    public async Task<AppResponse<ST.SharedKernel.Features.IAM.Users.Dtos.PasskeyLoginOptionsResponse>> Handle(RequestPasskeyLoginOptionsCommand request, CancellationToken cancellationToken)
    {
        AppUser? user = null;
        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var normalizedUsername = request.Username.ToUpperInvariant();
            user = await userManager.Users
                .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUsername || u.NormalizedEmail == normalizedUsername, cancellationToken);

            if (user == null || !user.IsActive || user.IsDeleted)
            {
                return AppResponses.Failure<ST.SharedKernel.Features.IAM.Users.Dtos.PasskeyLoginOptionsResponse>("Invalid login attempt.");
            }
        }

        var options = await passkeyService.RequestAssertionAsync(user?.UserName ?? string.Empty, cancellationToken);
        var correlationId = System.Guid.NewGuid();
        var cacheKey = $"Fido2AssertionOptions:{correlationId}";

        await cacheService.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(options),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = System.TimeSpan.FromMinutes(5) },
            cancellationToken);

        var response = new ST.SharedKernel.Features.IAM.Users.Dtos.PasskeyLoginOptionsResponse(options, correlationId);
        return AppResponses.Success("Login options generated.", response);
    }
}
