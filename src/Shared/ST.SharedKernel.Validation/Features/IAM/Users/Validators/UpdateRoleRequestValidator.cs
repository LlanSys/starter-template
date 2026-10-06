using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Validation.Validators.Common;
using FluentValidation;

namespace ST.SharedKernel.Validation.Features.IAM.Users.Validators;

public sealed class UpdateRoleRequestValidator : Validator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(80);
    }
}
