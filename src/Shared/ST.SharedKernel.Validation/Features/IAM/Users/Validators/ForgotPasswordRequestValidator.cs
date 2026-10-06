using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Validation.Validators.Common;
using FluentValidation;

namespace ST.SharedKernel.Validation.Features.IAM.Users.Validators;

public sealed class ForgotPasswordRequestValidator : Validator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}
