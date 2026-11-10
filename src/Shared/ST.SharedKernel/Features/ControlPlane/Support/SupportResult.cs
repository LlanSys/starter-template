using ST.SharedKernel.Features.IAM.Users.Dtos;
namespace ST.SharedKernel.Features.ControlPlane.Support;
public sealed record SupportResult(SupportGrantResponse Grant, LoginResponse? Session = null);
