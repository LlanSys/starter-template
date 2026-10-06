using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.Domain.Features.IAM.Users.Enums;
using ST.SharedKernel.Validation.Validators.Common;
using FluentValidation;
using System;

namespace ST.SharedKernel.Validation.Features.IAM.Users.Validators;

public sealed class SendEmailOtpRequestValidator : Validator<SendEmailOtpRequest>
{
    public SendEmailOtpRequestValidator()
    {
        RuleFor(request => request.UserId).NotEmpty().MaximumLength(450);
        RuleFor(request => request.Purpose)
            .NotEmpty()
            .Must(BeKnownOtpPurpose)
            .WithMessage("OTP purpose is not supported.");
    }

    private static bool BeKnownOtpPurpose(string purpose)
        => Enum.TryParse<OtpPurpose>(purpose, ignoreCase: true, out _);
}
