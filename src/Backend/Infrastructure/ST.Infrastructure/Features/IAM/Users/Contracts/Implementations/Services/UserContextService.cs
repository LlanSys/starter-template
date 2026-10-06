using ST.Application.Features.IAM.Users.Contracts.Dtos;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ST.Infrastructure.Features.IAM.Users.Contracts.Implementations.Services;


internal sealed class UserContextService(IHttpContextAccessor httpContextAccessor) : IUserContextService
{
    public UserIdentityContext GetCurrentContext()
    {
        var user = httpContextAccessor.HttpContext?.User
            ?? throw new InvalidOperationException("No active HTTP context.");

        return !user.Identity?.IsAuthenticated ?? true
            ? throw new UnauthorizedAccessException("User is not authenticated.")
            : UserIdentityContext.FromClaims(user);
    }
}
