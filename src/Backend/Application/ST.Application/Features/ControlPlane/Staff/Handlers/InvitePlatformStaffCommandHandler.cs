using MediatR;
using ST.Application.Features.ControlPlane.Staff.Commands;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Staff;

namespace ST.Application.Features.ControlPlane.Staff.Handlers;

internal sealed class InvitePlatformStaffCommandHandler(IPlatformStaffService service) : IRequestHandler<InvitePlatformStaffCommand, AppResponse<bool>>
{
    public Task<AppResponse<bool>> Handle(InvitePlatformStaffCommand request, CancellationToken cancellationToken)
    {
        return service.InviteAsync(request.UserId, request.Request, cancellationToken);
    }
}
