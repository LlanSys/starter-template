using ST.Application.Features.IAM.Users.Commands;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class GetOtpStatus(UserManager<AppUser> userManager, ILogger<GetOtpStatus> logger)
    : IRequestHandler<GetOtpStatusCommand, AppResponse<OtpStatusResponse>>
{
    public async Task<AppResponse<OtpStatusResponse>> Handle(GetOtpStatusCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var user = await userManager.FindByIdAsync(command.UserId).ConfigureAwait(false);
            if (user == null)
            {
                return AppResponses.Failure<OtpStatusResponse>("User not found.");
            }

            var isEnabled = await userManager.GetTwoFactorEnabledAsync(user).ConfigureAwait(false);

            var response = new OtpStatusResponse(
                IsConfigured: isEnabled,
                IsEnabled: isEnabled,
                ProviderName: "Authenticator",
                DisplayName: "Authenticator App"
            );

            return AppResponses.Success("OTP status retrieved", response);
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogGetOtpStatusError(logger, command.UserId, ex);
            throw;
        }
    }
}
