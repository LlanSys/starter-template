using FluentValidation;

namespace ST.Application.Features.ControlPlane.Tenants.Commands;

public class MigrateTenantStampCommandValidator : AbstractValidator<MigrateTenantStampCommand>
{
    public MigrateTenantStampCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.NewDeploymentStampId).NotEmpty();
        RuleFor(x => x.NewDatabaseConnectionString).NotEmpty();
    }
}
