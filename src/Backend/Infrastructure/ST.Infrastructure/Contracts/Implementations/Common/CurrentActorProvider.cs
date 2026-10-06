using ST.Domain.Shared.Contracts.Common;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ST.Infrastructure.Contracts.Implementations.Common;

internal sealed class CurrentActorProvider(IHttpContextAccessor httpContextAccessor) : ICurrentActorProvider
{
    public string ActorId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                return userId;
            }

            return ICurrentActorProvider.SystemActor;
        }
    }
}
