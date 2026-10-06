using FluentValidation;

namespace ST.Application.Features.ControlPlane.Tenants.Commands;

public class CompleteTenantProvisioningCommandValidator : AbstractValidator<CompleteTenantProvisioningCommand>
{
    public CompleteTenantProvisioningCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.DatabaseConnectionString).NotEmpty();
    }
}
