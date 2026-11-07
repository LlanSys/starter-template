using FluentValidation;
namespace ST.Application.Features.ControlPlane.Modules;
public sealed class ApplyRoleTemplateValidator : AbstractValidator<ApplyRoleTemplateCommand>
{
    public ApplyRoleTemplateValidator()
    {
        RuleFor(x => x.Request.TemplateKey).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Request.RoleName).NotEmpty().MaximumLength(120).Must(name => !name.Equals("System Administrator", StringComparison.OrdinalIgnoreCase)).WithMessage("This role name is reserved for platform setup.");
        RuleFor(x => x.Request.ExpectedVersion).GreaterThan(0);
    }
}
