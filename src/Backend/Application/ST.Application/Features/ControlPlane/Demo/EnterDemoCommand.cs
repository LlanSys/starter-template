using MediatR;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
namespace ST.Application.Features.ControlPlane.Demo;
public sealed record EnterDemoCommand(string ActorId, bool ReadOnly = false) : IRequest<AppResponse<LoginResponse>>;
