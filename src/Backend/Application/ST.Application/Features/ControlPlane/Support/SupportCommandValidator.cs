using FluentValidation;
namespace ST.Application.Features.ControlPlane.Support;
public sealed class SupportCommandValidator : AbstractValidator<SupportCommand>
{
 public SupportCommandValidator() {
 RuleFor(x=>x.Action).Must(x=>x is "request" or "approve" or "revoke" or "enter" or "exit");
 When(x=>x.Action=="request",()=> { RuleFor(x=>x.Request).NotNull();When(x=>x.Request!=null,()=> {
 RuleFor(x=>x.Request!.TenantId).NotEmpty();RuleFor(x=>x.Request!.Reason).NotEmpty().MaximumLength(1000);
 RuleFor(x=>x.Request!.DurationMinutes).InclusiveBetween(5,120);RuleFor(x=>x.Request!.PermissionKeys).NotNull().Must(x=>x.Length is >0 and <=100);
 }); });
 When(x=>x.Request is not null,()=> {
 RuleForEach(x=>x.Request!.PermissionKeys).NotEmpty().MaximumLength(160);
 RuleFor(x=>x.Request!.DurationMinutes).InclusiveBetween(5,120);
 });
 When(x=>x.Action!="request",()=>RuleFor(x=>x.GrantId).NotEmpty());
 }
}
