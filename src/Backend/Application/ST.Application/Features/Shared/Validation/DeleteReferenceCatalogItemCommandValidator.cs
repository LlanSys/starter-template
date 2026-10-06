using ST.Application.Features.IAM.ReferenceData.Commands;
using FluentValidation;

namespace ST.Application.Features.Shared.Validation;

public sealed class DeleteReferenceCatalogItemCommandValidator : AbstractValidator<DeleteReferenceCatalogItemCommand>
{
    public DeleteReferenceCatalogItemCommandValidator()
    {
        RuleFor(x => x.CatalogType).NotEmpty().WithMessage("Catalog type is required.");
        RuleFor(x => x.Id).NotEmpty().WithMessage("Catalog item ID is required.");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Current user is required.");
    }
}
