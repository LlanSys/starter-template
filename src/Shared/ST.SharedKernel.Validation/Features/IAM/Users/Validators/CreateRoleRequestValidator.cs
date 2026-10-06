using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Validation.Validators.Common;
using FluentValidation;

namespace ST.SharedKernel.Validation.Features.IAM.Users.Validators;

public sealed class CreateRoleRequestValidator : Validator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(80);
    }
}
