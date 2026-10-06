using ST.SharedKernel.Features.IAM.Permissions.Dtos;
using ST.SharedKernel.Validation.Validators.Common;
using FluentValidation;

namespace ST.SharedKernel.Validation.Features.IAM.Permissions.Validators;

public sealed class UpdatePermissionRequestValidator : Validator<UpdatePermissionRequest>
{
    public UpdatePermissionRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Permission ID is required.");

        RuleFor(x => new CreatePermissionRequest
        {
            DepartmentId = x.DepartmentId,
            Context = x.Context,
            Resource = x.Resource,
            Action = x.Action,
            Description = x.Description
        }).SetValidator(new CreatePermissionRequestValidator());
    }
}
