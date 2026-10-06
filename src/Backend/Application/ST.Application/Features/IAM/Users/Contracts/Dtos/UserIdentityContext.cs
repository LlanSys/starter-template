using System.Security.Claims;

namespace ST.Application.Features.IAM.Users.Contracts.Dtos;

public sealed record UserIdentityContext
{
    public string AppUserId { get; init; } = string.Empty;
    public Guid TenantId { get; init; }

    public static UserIdentityContext FromClaims(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        return new UserIdentityContext
        {
            AppUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            TenantId = Guid.TryParse(principal.FindFirstValue("tenant_id"), out var tenantId) ? tenantId : Guid.Empty
        };
    }
}
