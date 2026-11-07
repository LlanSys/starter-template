using MediatR;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Support;
namespace ST.Application.Features.ControlPlane.Support;
public sealed record SupportCommand(string Action, Guid? GrantId, SupportRequest? Request) : IRequest<AppResponse<SupportResult>>;
