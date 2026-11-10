using FluentValidation;
namespace ST.Application.Features.ControlPlane.Modules;
public sealed class SaveModuleFlagValidator : AbstractValidator<SaveModuleFlagCommand>
{
    public SaveModuleFlagValidator()
    {
        RuleFor(x => x.ModuleKey).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Request.Key).NotEmpty().MaximumLength(120).Matches("^[a-zA-Z0-9_.-]+$");
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(120);
    }
}
