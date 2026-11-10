using MediatR;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Features.ControlPlane.Modules.Dtos;
namespace ST.Application.Features.ControlPlane.Modules;
public sealed record ApplyRoleTemplateCommand(ApplyRoleTemplateRequest Request) : IRequest<AppResponse<AdminRoleListResponse>>;
