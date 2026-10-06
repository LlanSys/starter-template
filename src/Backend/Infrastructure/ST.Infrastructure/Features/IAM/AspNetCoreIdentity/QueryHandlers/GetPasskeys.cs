using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Application.Features.IAM.Users.Queries;
using ST.Domain.Features.IAM.Users.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.QueryHandlers;

internal sealed class GetPasskeys(
    IUserContextService userContext,
    UserManager<AppUser> userManager) : IRequestHandler<GetPasskeysQuery, AppResponse<IReadOnlyList<PasskeyResponse>>>
{
    public async Task<AppResponse<IReadOnlyList<PasskeyResponse>>> Handle(GetPasskeysQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userContext.GetCurrentContext().AppUserId))
        {
            return AppResponses.Failure<IReadOnlyList<PasskeyResponse>>("User must be authenticated.");
        }

        var user = await userManager.Users
            .Include(u => u.Fido2Credentials)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userContext.GetCurrentContext().AppUserId, cancellationToken)
            .ConfigureAwait(false);

        if (user == null)
        {
            return AppResponses.Failure<IReadOnlyList<PasskeyResponse>>("User not found.");
        }

        var passkeys = user.Fido2Credentials
            .Select(c => new PasskeyResponse(c.Id, "Passkey - " + c.RegDate.ToString("MMM dd, yyyy"), c.RegDate))
            .ToList();

        return AppResponses.Success<IReadOnlyList<PasskeyResponse>>(passkeys);
    }
}
