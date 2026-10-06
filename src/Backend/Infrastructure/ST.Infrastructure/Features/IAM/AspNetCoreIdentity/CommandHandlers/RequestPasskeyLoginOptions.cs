using ST.Domain.Shared.Contracts.Common;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Users.Entities;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class RequestPasskeyLoginOptions(
    IPasskeyService passkeyService,
    UserManager<AppUser> userManager,
    IIamUnitOfWork unitOfWork, ICurrentTenantProvider tenantProvider) : IRequestHandler<RequestPasskeyLoginOptionsCommand, AppResponse<ST.SharedKernel.Features.IAM.Users.Dtos.PasskeyLoginOptionsResponse>>
{
    public async Task<AppResponse<ST.SharedKernel.Features.IAM.Users.Dtos.PasskeyLoginOptionsResponse>> Handle(RequestPasskeyLoginOptionsCommand request, CancellationToken cancellationToken)
    {
        AppUser? user = null;
        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var normalizedUsername = request.Username.ToUpperInvariant();
            user = await userManager.Users
                .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUsername || u.NormalizedEmail == normalizedUsername, cancellationToken).ConfigureAwait(false);

            if (user == null || !user.IsActive || user.IsDeleted || !user.EmailConfirmed)
            {
                return AppResponses.Failure<ST.SharedKernel.Features.IAM.Users.Dtos.PasskeyLoginOptionsResponse>("Invalid login attempt.");
            }
        }

        var options = await passkeyService.RequestAssertionAsync(user?.UserName ?? string.Empty, cancellationToken).ConfigureAwait(false);
        var correlationId = System.Guid.CreateVersion7();
        await unitOfWork.PasskeyChallengeRepository.ReplaceAsync(new PasskeyChallenge
        {
            Key = PasskeyChallengeKeys.Assertion(correlationId),
            TenantId = tenantProvider.TenantId,
            OptionsJson = JsonSerializer.Serialize(options),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
            CreatedBy = user?.Id ?? ICurrentActorProvider.SystemActor
        }, cancellationToken).ConfigureAwait(false);
        await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

        var response = new ST.SharedKernel.Features.IAM.Users.Dtos.PasskeyLoginOptionsResponse(options, correlationId);
        return AppResponses.Success("Login options generated.", response);
    }
}
