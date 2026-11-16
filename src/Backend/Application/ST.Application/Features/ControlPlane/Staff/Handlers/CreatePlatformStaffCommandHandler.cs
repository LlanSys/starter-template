using MediatR;
using ST.Application.Features.ControlPlane.Staff.Commands;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Staff;

namespace ST.Application.Features.ControlPlane.Staff.Handlers;

internal sealed class CreatePlatformStaffCommandHandler(IPlatformStaffService service) : IRequestHandler<CreatePlatformStaffCommand, AppResponse<PlatformStaffResponse>>
{
    public Task<AppResponse<PlatformStaffResponse>> Handle(
        CreatePlatformStaffCommand request,
        CancellationToken cancellationToken
    )
    {
        return service.CreateAsync(request.Request, cancellationToken);
    }
}
