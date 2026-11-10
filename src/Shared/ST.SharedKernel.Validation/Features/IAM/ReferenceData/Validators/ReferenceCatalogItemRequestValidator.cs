using ST.SharedKernel.Features.IAM.ReferenceData.Dtos;
using ST.SharedKernel.Validation.Validators.Common;
using FluentValidation;

namespace ST.SharedKernel.Validation.Features.IAM.ReferenceData.Validators;

public sealed class ReferenceCatalogItemRequestValidator : Validator<ReferenceCatalogItemRequest>
{
    public ReferenceCatalogItemRequestValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty().WithMessage("Catalog key is required.")
            .MaximumLength(120).WithMessage("Catalog key cannot exceed 120 characters.")
            .Matches("^[a-z0-9_.:-]+$").WithMessage("Catalog key must use lowercase letters, numbers, dots, underscores, colons, or hyphens.");

        RuleFor(x => x.Label)
            .NotEmpty().WithMessage("Catalog label is required.")
            .MaximumLength(150).WithMessage("Catalog label cannot exceed 150 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Catalog description cannot exceed 500 characters.");

        RuleFor(x => x.ParentKey)
            .MaximumLength(120).WithMessage("Parent key cannot exceed 120 characters.")
            .Matches("^[a-z0-9_.:-]+$").WithMessage("Parent key must use lowercase letters, numbers, dots, underscores, colons, or hyphens.")
            .When(x => !string.IsNullOrWhiteSpace(x.ParentKey));

        RuleFor(x => x.Url)
            .MaximumLength(300).WithMessage("URL cannot exceed 300 characters.")
            .Must(BeRelativeRoute)
            .WithMessage("URL must be an application route such as /admin/users.")
            .When(x => !string.IsNullOrWhiteSpace(x.Url));
    }

    private static bool BeRelativeRoute(string route)
    {
        // Rooted application routes are classified as absolute file URIs on Unix.
        return route.Length > 0
            && route[0] == '/'
            && !route.StartsWith("//", StringComparison.Ordinal)
            && !route.Contains('\\')
            && !route.Any(char.IsControl)
            && Uri.TryCreate(route, UriKind.Relative, out _);
    }
}
