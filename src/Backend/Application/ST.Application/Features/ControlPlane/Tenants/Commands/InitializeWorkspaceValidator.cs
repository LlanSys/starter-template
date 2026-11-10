using FluentValidation;
namespace ST.Application.Features.ControlPlane.Tenants.Commands;
public sealed class InitializeWorkspaceValidator : AbstractValidator<InitializeWorkspaceCommand>
{
 public InitializeWorkspaceValidator() {
 RuleFor(x=>x.TenantId).NotEmpty(); RuleFor(x=>x.Request).NotNull();
 RuleFor(x=>x.Request.AdministratorEmail).NotEmpty().EmailAddress().MaximumLength(256);
 RuleFor(x=>x.Request.FirstName).NotEmpty().MaximumLength(100); RuleFor(x=>x.Request.LastName).NotEmpty().MaximumLength(100);
 }
}
