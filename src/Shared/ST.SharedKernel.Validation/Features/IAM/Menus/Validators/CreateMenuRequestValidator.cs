using ST.SharedKernel.Features.IAM.Menus.Dtos;
using ST.SharedKernel.Validation.Validators.Common;
using FluentValidation;

namespace ST.SharedKernel.Validation.Features.IAM.Menus.Validators;

public sealed class CreateMenuRequestValidator : Validator<CreateMenuRequest>
{
    public CreateMenuRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Menu title is required.")
            .MaximumLength(120).WithMessage("Menu title cannot exceed 120 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Menu description cannot exceed 500 characters.");

        RuleFor(x => x.Url)
            .MaximumLength(300).WithMessage("Menu URL cannot exceed 300 characters.")
            .Must(BeRelativeRoute)
            .WithMessage("Menu URL must be an application route such as /admin/users.")
            .When(x => !string.IsNullOrWhiteSpace(x.Url));

        RuleFor(x => x.Icon)
            .NotEmpty().WithMessage("Menu icon is required.")
            .MaximumLength(80).WithMessage("Menu icon cannot exceed 80 characters.");

        RuleFor(x => x.Placement)
            .NotEmpty().WithMessage("Menu placement is required.")
            .MaximumLength(80).WithMessage("Menu placement cannot exceed 80 characters.");

        RuleFor(x => x.RequiredPermissionKey)
            .MaximumLength(150).WithMessage("Required permission key cannot exceed 150 characters.")
            .Matches("^[a-z0-9_.:-]+$").WithMessage("Required permission key must use lowercase letters, numbers, dots, underscores, colons, or hyphens.")
            .When(x => !string.IsNullOrWhiteSpace(x.RequiredPermissionKey));
    }

    private static bool BeRelativeRoute(string route)
        => route.Length > 0
            && route[0] == '/'
            && !route.StartsWith("//", StringComparison.Ordinal)
            && !Uri.TryCreate(route, UriKind.Absolute, out _);
}
