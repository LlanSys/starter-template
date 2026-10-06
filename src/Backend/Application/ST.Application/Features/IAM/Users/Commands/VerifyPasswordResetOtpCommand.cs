using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;

namespace ST.Application.Features.IAM.Users.Commands;

public sealed record VerifyPasswordResetOtpCommand(VerifyPasswordResetOtpRequest Request)
    : IRequest<AppResponse<PasswordResetOtpVerificationResponse>>;
