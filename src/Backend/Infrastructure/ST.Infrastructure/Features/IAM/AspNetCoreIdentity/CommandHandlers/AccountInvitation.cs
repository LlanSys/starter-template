using ST.Domain.Features.IAM.Users.Entities;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal static class AccountInvitation
{
    // The Identity provider also binds the token to the account ID and current security stamp.
    internal static string Purpose(AppUser user) => $"AccountInvitation:{user.TenantId:D}:{user.Email}";
}
