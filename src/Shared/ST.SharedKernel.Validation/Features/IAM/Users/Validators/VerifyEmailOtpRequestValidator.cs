using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.Domain.Features.IAM.Users.Enums;
using ST.SharedKernel.Validation.Validators.Common;
using FluentValidation;
using System;

namespace ST.SharedKernel.Validation.Features.IAM.Users.Validators;

public sealed class VerifyEmailOtpRequestValidator : Validator<VerifyEmailOtpRequest>
{
    public VerifyEmailOtpRequestValidator()
    {
        RuleFor(request => request.UserId).NotEmpty().MaximumLength(450);
        RuleFor(request => request.Code).NotEmpty().Matches("^\\d{6}$");
        RuleFor(request => request.Purpose)
            .NotEmpty()
            .Must(static purpose => Enum.TryParse<OtpPurpose>(purpose, ignoreCase: true, out _))
            .WithMessage("OTP purpose is not supported.");
        RuleFor(request => request.DeviceFingerprint).MaximumLength(256);
    }
}
