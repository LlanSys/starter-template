using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Validation.Validators.Common;
using FluentValidation;

namespace ST.SharedKernel.Validation.Features.IAM.Users.Validators;

public sealed class VerifyPasswordResetOtpRequestValidator : Validator<VerifyPasswordResetOtpRequest>
{
    public VerifyPasswordResetOtpRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(request => request.Code).NotEmpty().Matches("^\\d{6}$");
    }
}
