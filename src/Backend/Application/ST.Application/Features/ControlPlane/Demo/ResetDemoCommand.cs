using MediatR;
using ST.SharedKernel.Dtos.Common;
namespace ST.Application.Features.ControlPlane.Demo;
public sealed record ResetDemoCommand(string ActorId) : IRequest<AppResponse<bool>>;
