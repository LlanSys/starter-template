using ST.SharedKernel.Features.IAM.Menus.Dtos;
using ST.SharedKernel.Validation.Validators.Common;
using FluentValidation;

namespace ST.SharedKernel.Validation.Features.IAM.Menus.Validators;

public sealed class UpdateMenuRequestValidator : Validator<UpdateMenuRequest>
{
    public UpdateMenuRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Menu ID is required.");

        RuleFor(x => new CreateMenuRequest
        {
            ParentId = x.ParentId,

            Title = x.Title,
            Description = x.Description,
            Url = x.Url,
            Icon = x.Icon,
            Placement = x.Placement,
            RequiredPermissionKey = x.RequiredPermissionKey
        }).SetValidator(new CreateMenuRequestValidator());
    }
}
