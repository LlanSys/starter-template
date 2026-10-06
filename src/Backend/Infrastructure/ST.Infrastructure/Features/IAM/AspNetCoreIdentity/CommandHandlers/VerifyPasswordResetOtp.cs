using ST.Application.Features.IAM.Users.Commands;
using ST.Domain.Features.IAM.Users.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class VerifyPasswordResetOtp(UserManager<AppUser> userManager, ISender sender)
    : IRequestHandler<VerifyPasswordResetOtpCommand, AppResponse<PasswordResetOtpVerificationResponse>>
{
    public async Task<AppResponse<PasswordResetOtpVerificationResponse>> Handle(
        VerifyPasswordResetOtpCommand command,
        CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(command.Request.Email).ConfigureAwait(false);
        if (user is null || !user.IsActive || user.IsDeleted)
        {
            return AppResponses.Failure<PasswordResetOtpVerificationResponse>("The recovery code is invalid or expired.");
        }

        var verification = await sender.Send(new VerifyEmailOtpCommand(new VerifyEmailOtpRequest
        {
            UserId = user.Id,
            Code = command.Request.Code,
            Purpose = "PasswordReset"
        }), ct).ConfigureAwait(false);

        if (!verification.IsSuccess)
        {
            return AppResponses.Failure<PasswordResetOtpVerificationResponse>("The recovery code is invalid or expired.");
        }

        var transitionToken = await userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
        return AppResponses.Success(
            "Recovery code verified.",
            new PasswordResetOtpVerificationResponse(transitionToken));
    }
}
