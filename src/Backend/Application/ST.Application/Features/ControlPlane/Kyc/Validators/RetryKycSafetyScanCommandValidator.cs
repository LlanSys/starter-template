using FluentValidation;
using ST.Application.Features.ControlPlane.Kyc.Commands;
namespace ST.Application.Features.ControlPlane.Kyc.Validators;
public sealed class RetryKycSafetyScanCommandValidator : AbstractValidator<RetryKycSafetyScanCommand>
{
    public RetryKycSafetyScanCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.Revision).NotEmpty().When(x => x.Request is not null);
    }
}
