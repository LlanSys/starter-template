using FluentValidation;
using ST.SharedKernel.Features.Shared.OrgSettings;
namespace ST.Application.Features.Shared.OrgSettings.SaveCatalogSetting;
public sealed class SaveCatalogOrgSettingCommandValidator : AbstractValidator<SaveCatalogOrgSettingCommand>
{
    public SaveCatalogOrgSettingCommandValidator()
    {
        RuleFor(command => command.Key).Must(key => OrganizationSettingCatalog.Find(key) is not null).WithMessage("Choose a supported organization setting.");
        RuleFor(command => command).Must(command => command.Reset || OrganizationSettingCatalog.IsValid(command.Key, command.Value))
            .WithMessage("Choose a supported value for this setting.");

        RuleFor(command => command.Revision).MaximumLength(128);
    }
}
