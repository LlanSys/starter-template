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

internal sealed class RegisterPasskey(
    IPasskeyService passkeyService,
    IUserContextService userContext,
    UserManager<AppUser> userManager,
    IIamUnitOfWork unitOfWork,
    IDistributedCache cacheService) : IRequestHandler<RegisterPasskeyCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(RegisterPasskeyCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userContext.GetCurrentContext().AppUserId))
        {
            return AppResponses.Failure<bool>("User must be authenticated to register a passkey.");
        }

        var user = await userManager.Users
            .FirstOrDefaultAsync(u => u.Id == userContext.GetCurrentContext().AppUserId, cancellationToken)
            .ConfigureAwait(false);

        if (user == null)
        {
            return AppResponses.Failure<bool>("User not found.");
        }

        var cacheKey = $"Fido2RegistrationOptions:{user.Id}";
        var originalOptionsStr = await cacheService.GetStringAsync(cacheKey, cancellationToken).ConfigureAwait(false);
        JsonElement originalOptions = default;

        if (string.IsNullOrEmpty(originalOptionsStr))
        {
            return AppResponses.Failure<bool>("Registration options have expired or do not exist.");
        }
        else
        {
            originalOptions = JsonSerializer.Deserialize<JsonElement>(originalOptionsStr);
        }

        try
        {
            var credential = await passkeyService.MakeNewCredentialAsync(user, request.AttestationResponse, originalOptions, cancellationToken).ConfigureAwait(false);

            await unitOfWork.Fido2CredentialRepository.CreateAsync(credential, cancellationToken).ConfigureAwait(false);
            await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

            await cacheService.RemoveAsync(cacheKey, cancellationToken).ConfigureAwait(false);

            return AppResponses.Success("Passkey registered successfully.", true);
        }
        catch (System.Exception ex)
        {
            return AppResponses.Failure<bool>($"Failed to register passkey: {ex.Message}");
        }
    }
}
