using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace ST.Application.Features.IAM.Users.Contracts.Dtos;

public sealed record UserIdentityContext
{
    public string AppUserId { get; init; } = string.Empty;
    public Guid TenantId { get; init; }
    public Guid? EmployeeId { get; init; }
    public Guid? CustomerId { get; init; }
    public bool IsEmployee => EmployeeId.HasValue;
    public bool IsCustomer => CustomerId.HasValue;
    public bool IsDualRole => IsEmployee && IsCustomer;
    public string ActiveContext { get; init; } = string.Empty; // "Employee" | "Customer"

    // Convenience — used by Blazor components for AuthorizeView policies
    public bool IsInContext(string context) =>
        ActiveContext.Equals(context, StringComparison.OrdinalIgnoreCase);

    // Used during onboarding: "can this NationalId be linked as employee?"
    public bool CanLinkAsEmployee => !IsEmployee;
    public bool CanLinkAsCustomer => !IsCustomer;

    public static UserIdentityContext FromClaims(ClaimsPrincipal principal) => new()
    {
        AppUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
        TenantId = Guid.TryParse(principal.FindFirstValue("tenant_id"), out var tid) ? tid : Guid.Empty,
        EmployeeId = Guid.TryParse(principal.FindFirstValue("employee_id"), out var eid) ? eid : null,
        CustomerId = Guid.TryParse(principal.FindFirstValue("customer_id"), out var cid) ? cid : null,
        ActiveContext = principal.FindFirstValue("active_context") ?? string.Empty
    };
}
