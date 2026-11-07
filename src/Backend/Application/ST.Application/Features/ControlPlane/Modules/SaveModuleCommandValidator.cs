using FluentValidation;
namespace ST.Application.Features.ControlPlane.Modules;
public sealed class SaveModuleCommandValidator : AbstractValidator<SaveModuleCommand>
{
    public SaveModuleCommandValidator()
    {
        RuleFor(command => command.Request).NotNull();
        RuleFor(command => command.Request.Key).NotEmpty().MaximumLength(80).Matches("^[A-Za-z][A-Za-z0-9_-]*$");
        RuleFor(command => command.Request.Name).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Request.Description).NotNull().MaximumLength(500);
    }
}
