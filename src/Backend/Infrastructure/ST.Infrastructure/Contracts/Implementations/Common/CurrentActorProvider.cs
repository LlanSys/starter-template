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
            if (user?.HasClaim("demo_workspace", "true") == true && user.FindFirstValue("demo_actor_id") is string demoActor) return demoActor;
            if (user?.FindFirstValue("support_actor_id") is string supportActor && user.HasClaim(x => x.Type == "support_grant_id")) return supportActor;
            var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                return userId;
            }

            return ICurrentActorProvider.SystemActor;
        }
    }
}
