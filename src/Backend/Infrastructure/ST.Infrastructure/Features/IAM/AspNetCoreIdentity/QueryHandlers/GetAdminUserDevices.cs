using ST.Application.Features.IAM.Users.Queries;
using ST.Persistence.Features.IAM.DataContext;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.QueryHandlers;

internal sealed class GetAdminUserDevices(IamDBContext context)
    : IRequestHandler<GetAdminUserDevicesQuery, AppResponse<IReadOnlyList<AdminUserDeviceResponse>>>
{
    public async Task<AppResponse<IReadOnlyList<AdminUserDeviceResponse>>> Handle(GetAdminUserDevicesQuery request, CancellationToken cancellationToken)
    {
        var devices = await context.AppUserDevices
            .AsNoTracking()
            .Include(static device => device.AppUser)
            .OrderBy(static device => device.AppUser.Email)
            .ThenBy(static device => device.DeviceName)
            .Select(device => new AdminUserDeviceResponse(
                device.Id,
                device.AppUserId,
                device.AppUser.UserName ?? string.Empty,
                device.AppUser.Email ?? string.Empty,
                device.DeviceName,
                device.IpAddress,
                device.IsTrusted,
                device.LastUsedAt,
                device.TrustedUntil))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return AppResponses.Success("User devices loaded.", (IReadOnlyList<AdminUserDeviceResponse>)devices);
    }
}
