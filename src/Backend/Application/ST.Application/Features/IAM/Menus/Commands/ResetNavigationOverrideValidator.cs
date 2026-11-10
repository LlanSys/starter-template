using FluentValidation;
namespace ST.Application.Features.IAM.Menus.Commands;
public sealed class ResetNavigationOverrideValidator : AbstractValidator<ResetNavigationOverrideCommand>
{
    public ResetNavigationOverrideValidator() => RuleFor(x => x.PageKey).NotEmpty().MaximumLength(160);
}
