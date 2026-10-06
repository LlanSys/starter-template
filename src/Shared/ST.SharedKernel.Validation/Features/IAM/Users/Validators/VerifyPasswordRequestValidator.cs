using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Validation.Validators.Common;
using FluentValidation;

namespace ST.SharedKernel.Validation.Features.IAM.Users.Validators;

public sealed class VerifyPasswordRequestValidator : Validator<VerifyPasswordRequest>
{
    public VerifyPasswordRequestValidator()
    {
        RuleFor(request => request.UserId).NotEmpty().MaximumLength(450);
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(request => request.EmployeeNumber).MaximumLength(50);
        RuleFor(request => request.Password).NotEmpty().MaximumLength(256);
    }
}
