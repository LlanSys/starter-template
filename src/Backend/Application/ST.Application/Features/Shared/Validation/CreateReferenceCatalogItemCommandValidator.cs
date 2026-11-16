using ST.Application.Features.IAM.ReferenceData.Commands;
using ST.SharedKernel.Validation.Features.IAM.ReferenceData.Validators;
using FluentValidation;

namespace ST.Application.Features.Shared.Validation;

public sealed class CreateReferenceCatalogItemCommandValidator : AbstractValidator<CreateReferenceCatalogItemCommand>
{
    public CreateReferenceCatalogItemCommandValidator()
    {
        RuleFor(x => x.CatalogType).NotEmpty().WithMessage("Catalog type is required.");
        RuleFor(x => x.Request).SetValidator(command => new ReferenceCatalogItemRequestValidator(
            string.Equals(command.CatalogType, "menu-icons", StringComparison.OrdinalIgnoreCase)));
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Current user is required.");
    }
}
