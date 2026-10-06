using ST.Application.Features.IAM.Users.Commands;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class VerifyPassword(
    UserManager<AppUser> userManager,
    ILogger<VerifyPassword> logger) : IRequestHandler<VerifyPasswordCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(VerifyPasswordCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        try
        {
            var user = await userManager.FindByIdAsync(request.UserId).ConfigureAwait(false);
            var userByEmail = await userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);

            if (user == null && userByEmail != null)
            {
                user = userByEmail;
            }

            if (user == null)
            {
                return AppResponses.Failure<bool>("User not found");
            }

            if (await userManager.IsLockedOutAsync(user).ConfigureAwait(false))
            {
                return AppResponses.Failure<bool>("Account is locked");
            }

            var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password).ConfigureAwait(false);
            if (!isPasswordValid)
            {
                await userManager.AccessFailedAsync(user).ConfigureAwait(false);

                return await userManager.IsLockedOutAsync(user).ConfigureAwait(false)
                    ? AppResponses.Failure<bool>("Account locked due to too many failed attempts")
                    : AppResponses.Failure<bool>("Invalid password");
            }

            await userManager.ResetAccessFailedCountAsync(user).ConfigureAwait(false);

            return AppResponses.Success("Password verified successfully", true);
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogErrorVerifyingPassword(logger, ex);
            throw;
        }
    }
}
