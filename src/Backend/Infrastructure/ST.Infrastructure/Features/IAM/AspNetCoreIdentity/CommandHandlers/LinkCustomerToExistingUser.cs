using ST.SharedKernel.Extensions;
using ST.Application.Features.IAM.Users.Commands;
using ST.Domain.Features.Banking.Customers.Entities;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Features.IAM.Users.Enums;
using ST.SharedKernel.Features.Banking.Customers.Dtos;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class LinkCustomerToExistingUser(UserManager<AppUser> userManager)
    : IRequestHandler<LinkCustomerToExistingUserCommand, AppResponse<CustomerResponse>>
{
    public async Task<AppResponse<CustomerResponse>> Handle(LinkCustomerToExistingUserCommand command, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(command.AppUserId).ConfigureAwait(false);

        if (user is null)
            return AppResponses.Failure<CustomerResponse>("User not found.");

        if (user.CustomerId.HasValue)
            return AppResponses.Failure<CustomerResponse>("User is already linked to a customer record.");

        user.LinkToCustomer(command.CustomerId);

        await userManager.UpdateAsync(user).ConfigureAwait(false);
        await userManager.AddToRoleAsync(user, Roles.Customer.ToDisplayString()).ConfigureAwait(false);

        return AppResponses.Success<CustomerResponse>("Customer account linked to user.", default!);
    }
}
