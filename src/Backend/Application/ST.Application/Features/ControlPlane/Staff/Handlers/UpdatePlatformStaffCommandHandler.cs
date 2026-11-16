using MediatR;
using ST.Application.Features.ControlPlane.Staff.Commands;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Staff;

namespace ST.Application.Features.ControlPlane.Staff.Handlers;

internal sealed class UpdatePlatformStaffCommandHandler(IPlatformStaffService service) : IRequestHandler<UpdatePlatformStaffCommand, AppResponse<PlatformStaffResponse>>
{
    public Task<AppResponse<PlatformStaffResponse>> Handle(
        UpdatePlatformStaffCommand request,
        CancellationToken cancellationToken
    )
    {
        return service.UpdateAsync(request.UserId, request.Request, cancellationToken);
    }
}
