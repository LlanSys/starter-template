using FluentValidation;
namespace ST.Application.Features.ControlPlane.Demo;
public sealed class ResetDemoValidator : AbstractValidator<ResetDemoCommand>
{
 public ResetDemoValidator() => RuleFor(x=>x.ActorId).NotEmpty();
}
