using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Validation.Validators.Common;
using FluentValidation;

namespace ST.SharedKernel.Validation.Features.IAM.Users.Validators;

public sealed class UpdateAdminUserRequestValidator : Validator<UpdateAdminUserRequest>
{
    public UpdateAdminUserRequestValidator()
    {
        RuleFor(request => request.UserName).NotEmpty().MaximumLength(50);
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(request => request.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(request => request.LastName).NotEmpty().MaximumLength(50);
        RuleFor(request => request.PhoneNumber).MaximumLength(32);
        RuleFor(request => request.Roles).NotNull().Must(roles => roles is null || roles.Count <= 32)
            .WithMessage("Select at most 32 roles.");
        RuleForEach(request => request.Roles).NotEmpty().MaximumLength(80);
    }
}
