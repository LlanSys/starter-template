using FluentValidation;
using ST.Application.Features.ControlPlane.Tenants.Commands;
using ST.SharedKernel.Features.ControlPlane.Tenants;
namespace ST.Application.Features.ControlPlane.Tenants.Validators;

public sealed class SaveTenantFrontendAddressCommandValidator : AbstractValidator<SaveTenantFrontendAddressCommand>
{
    public SaveTenantFrontendAddressCommandValidator()
    {
        RuleFor(request => request.Id).NotEmpty();
        RuleFor(request => request.Request.Origin).Must(origin => origin is null || FrontendOrigin.TryNormalize(origin, out _))
            .WithMessage("Enter an HTTPS app origin, or a local HTTP origin, without a path, query or credentials.");

        RuleFor(request => request.Request.Revision).NotEmpty().MaximumLength(128);
    }
}
