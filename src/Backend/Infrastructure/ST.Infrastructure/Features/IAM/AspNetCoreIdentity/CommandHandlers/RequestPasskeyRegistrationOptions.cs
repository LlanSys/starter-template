using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Features.IAM.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.Application.Contracts.Interfaces.Common;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class RequestPasskeyRegistrationOptions(
    IPasskeyService passkeyService,
    IUserContextService userContext,
    UserManager<AppUser> userManager,
    IDistributedCache cacheService) : IRequestHandler<RequestPasskeyRegistrationOptionsCommand, AppResponse<JsonElement>>
{
    public async Task<AppResponse<JsonElement>> Handle(RequestPasskeyRegistrationOptionsCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userContext.GetCurrentContext().AppUserId))
        {
            return AppResponses.Failure<JsonElement>("User must be authenticated to register a passkey.");
        }

        var user = await userManager.Users
            .Include(u => u.Fido2Credentials)
            .FirstOrDefaultAsync(u => u.Id == userContext.GetCurrentContext().AppUserId, cancellationToken).ConfigureAwait(false);

        if (user == null)
        {
            return AppResponses.Failure<JsonElement>("User not found.");
        }

        var options = await passkeyService.RequestNewCredentialAsync(user, user.Fido2Credentials, cancellationToken).ConfigureAwait(false);

        // Cache the options for 5 minutes to verify against the response in the next step
        var cacheKey = $"Fido2RegistrationOptions:{user.Id}";
        await cacheService.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(options),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
            cancellationToken).ConfigureAwait(false);

        return AppResponses.Success("Registration options generated.", options);
    }
}
