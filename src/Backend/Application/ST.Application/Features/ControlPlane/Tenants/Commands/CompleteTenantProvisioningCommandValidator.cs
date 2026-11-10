using FluentValidation;

namespace ST.Application.Features.ControlPlane.Tenants.Commands;

public sealed class CompleteTenantProvisioningCommandValidator : AbstractValidator<CompleteTenantProvisioningCommand>
{
    public CompleteTenantProvisioningCommandValidator()
    {
        RuleFor(request => request.TenantId).NotEmpty();
        RuleFor(request => request.OperationId).NotNull().NotEqual(Guid.Empty);
        RuleFor(request => request.DatabaseConnectionString)
            .NotEmpty().MaximumLength(4096).When(request => request.Succeeded);
        RuleFor(request => request.DatabaseProvider)
            .Must(provider => provider is null
                || string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase)
                || string.Equals(provider, "PostgreSql", StringComparison.OrdinalIgnoreCase));
    }
}
