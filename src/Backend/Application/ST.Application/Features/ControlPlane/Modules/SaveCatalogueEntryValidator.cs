using FluentValidation;
using ST.Domain.Features.ControlPlane.Modules.Entities;
namespace ST.Application.Features.ControlPlane.Modules;
public sealed class SaveCatalogueEntryValidator : AbstractValidator<SaveCatalogueEntryCommand>
{
    public SaveCatalogueEntryValidator()
    {
        RuleFor(x => x.ModuleKey).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Request.Key).NotEmpty().MaximumLength(160).Matches("^[a-zA-Z0-9_.-]+$");
        RuleFor(x => x.Request.Title).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Request.Kind).Must(kind => Enum.TryParse<ModuleCatalogueKind>(kind, true, out var parsed) && Enum.IsDefined(parsed)).WithMessage("Select a valid catalogue type.");
        RuleFor(x => x.Request.Path).MaximumLength(240);
        RuleFor(x => x.Request.Icon).Must(ST.SharedKernel.Features.IAM.Menus.MaterialIconKeys.Contains).WithMessage("Select a registered icon.");
        RuleFor(x => x.Request.PermissionKeys).Must(keys => keys is null || keys.Length <= 100);
        RuleFor(x => x.Request.RoleNames).Must(keys => keys is null || keys.Length <= 100);
    }
}
