using MediatR;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Staff;

namespace ST.Application.Features.ControlPlane.Staff.Commands;

public sealed record CreatePlatformStaffCommand(CreatePlatformStaffRequest Request) : IRequest<AppResponse<PlatformStaffResponse>>;
