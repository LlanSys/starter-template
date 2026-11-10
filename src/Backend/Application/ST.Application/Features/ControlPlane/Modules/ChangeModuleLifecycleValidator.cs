using FluentValidation;
namespace ST.Application.Features.ControlPlane.Modules;
public sealed class ChangeModuleLifecycleValidator : AbstractValidator<ChangeModuleLifecycleCommand>
{
    public ChangeModuleLifecycleValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Request).NotNull();
        RuleFor(command => command.Request.Action).IsInEnum();
    }
}
