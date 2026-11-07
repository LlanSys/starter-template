using ST.Application.Features.IAM.Users.Commands;
using ST.Infrastructure.Logging;
using ST.Persistence.Features.IAM.DataContext;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class RevokeUserDevice(IamDBContext context, ILogger<RevokeUserDevice> logger, TimeProvider timeProvider)
    : IRequestHandler<RevokeUserDeviceCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(RevokeUserDeviceCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var device = await context.AppUserDevices
                .SingleOrDefaultAsync(item => item.Id == command.DeviceId, cancellationToken)
                .ConfigureAwait(false);

            if (device is null)
            {
                return AppResponses.Failure<bool>("User device not found.");
            }

            device.RevokeTrust(timeProvider.GetUtcNow(), command.RevokedBy);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return AppResponses.Success("Device trust revoked.", true);
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogUserDeviceRevokeError(logger, command.DeviceId.ToString(), ex);
            throw;
        }
    }
}
