# UI-to-Backend Flow

The default product-building flow is:

1. Start from the UI journey.
2. Call backend APIs through typed HTTP clients.
3. Keep DTO contracts in `ST.SharedKernel`.
4. Keep UI state, presentation models, and browser/session concerns in the UI layer.
5. Push business rules into Application and Domain when the journey exposes them.

## Supported frontend

ST.UI.Blazor is the sole frontend host. It uses global Interactive Server rendering with prerendering disabled. The interactive router restores protected browser tokens before rendering AuthorizeRouteView. The HTTP Razor endpoint serves an anonymous shell, while page policies govern the circuit and permission-protected API endpoints govern all data. No authorized content is prerendered into the shell. Components execute on the server over a SignalR circuit; typed HTTP clients call the separately hosted API. UI service contracts live in the corresponding Blazor feature Contracts/Interfaces folders. API DTOs remain in ST.SharedKernel. No MAUI workloads or separate Razor Class Library are required.

## Default Rule

Frontend projects must not reference backend Application, Domain, Persistence, EF Core contexts, repositories, or MediatR handlers directly.

Use this boundary:

```text
Blazor UI -> typed HTTP client -> ST.Api -> Application -> Domain -> Persistence/Infrastructure
```

This keeps the API contract real from day one. The same backend endpoints can later serve Blazor, mobile, external API clients, API Management, integration tests, and deployment probes.

## Why Not Call Application Directly?

Direct Application calls can reduce HTTP serialization overhead, but they also couple the UI deployment to backend internals. That coupling makes these things harder:

- Independent API/UI deployment.
- Azure API Management adoption.
- Mobile and external client reuse.
- Authentication and authorization consistency.
- Contract testing.
- Observability at the API boundary.
- Versioning and backward compatibility.

For this base template, the HTTP boundary is the enterprise default.

## Allowed Exception

Direct in-process Application calls are allowed only when a module is explicitly designed as one deployable unit and does not need to expose the same capability to API, mobile, external clients, or API Management.

Document the exception before using it.

## IAM transport and authentication state

Account/login/current-user/admin-list responses describe platform identity and contact data. They do not expose Employee/Customer links, national ID numbers or HR Gender. Current-user state includes the account's TenantId alongside session, permissions, roles and MFA enrollment status.

All backend account responses use AppUserMapping so password, OTP, TOTP, passkey, SSO, refresh and account administration agree on identity fields and actual account flags. Profile picture URLs remain API-mediated private asset routes. Blazor login fallbacks use LoginResponseMapping, preserving email confirmation and authenticated/permission state instead of manufacturing successful flags.

The response changes are intentional for the in-progress fresh StarterTemplate baseline. API and clients must use the same contract revision. Existing downstream database or client upgrades require their own compatibility decisions; no transitional business fields are shipped in these responses.

## Account access lifecycle

Create an account independently of HR/Banking, then activate it through PATCH iam/admin/users/{userId}/activate (users.edit permission). The account action preserves its password, email-confirmation state and role memberships. Email verification is still required by password login; activation does not manufacture that verification or deliver an invitation. Invitation/email-confirmation delivery and browser certification remain pending.

PATCH iam/admin/users/{userId}/deactivate (users.deactivate permission) disables the account and terminates its sessions and refresh tokens in one IAM transaction. Both operations require the resolved tenant to match the account and reject deleted accounts. Access events use account, tenant and audit actor identifiers.

Session validation treats the signed JWT session_id claim as authoritative over a request header or cookie. Revoked API sessions return 401 directly; web requests retain cookie sign-out. Existing public/temporary-token routing is unchanged.
## UI presentation

Authentication pages use one centred AuthLayout card with restrained headings. Alternate sign-in actions share one row with icons. Google remains disabled until a backend provider is implemented and configured; Microsoft uses the existing EntraId challenge and passkeys retain the existing WebAuthn flow. Account actions belong in the top-right header and show the name without a role. Sidebar branding is plain and menu items retain inset spacing. Preserve actionable validation, MFA and impersonation notices.

## Menu icons and destinations

MenuRoutes are database records initially seeded by IamReferenceDataSeed.MenuRoutes and maintained through the Access Catalog. Registration does not create a page or grant route authorization. The menu icon picker searches all installed MudBlazor Material styles with previews. Legacy unqualified keys resolve as Filled icons. Backend validation accepts the shared MaterialIconKeys allowlist or existing active database icon registrations. Regenerate the shared allowlist after upgrading MudBlazor with scripts/update-material-icon-keys.ps1 -MudBlazorAssemblyPath <installed MudBlazor.dll>; no SVG is accepted from the client.

## Menu visibility administration

Create/Edit Menu uses title, placement, optional parent, a searchable registered Page, an icon picker, and searchable Roles/Permissions selections shown as removable chips. The technical key is generated internally. Module and description inputs are not presented; existing stored values are retained on edits.

Visibility requires any selected role and all selected permissions. Empty selections impose no additional restriction. Existing System Administrator permission bypass remains, except control_plane.manage always requires its explicit permission; role restrictions still apply. Legacy RequiredPermissionKey is still enforced and appears in the editable selection. Role names are taken from authenticated claims and are included in the navigation cache discriminator. Page/API authorization continues to enforce actual access. Option loading uses the existing roles.view and roles.manage_permissions endpoints; Save remains disabled if either lookup fails.

RequiredRoleNames and RequiredPermissionKeys are EF primitive collections: SQL Server JSON columns and PostgreSQL text arrays. Provider-specific MenuVisibility migrations add empty collections to existing rows without removing legacy module/permission restrictions. The SQL Server migration was applied only to the local ST database on localhost:14335. PostgreSQL migration SQL was generated/tested, not applied to a running PostgreSQL server.

## Platform navigation and module availability

Control Panel owns the Deployment Stamps/Tenants/Modules table tabs. Payments has its own /payments page and module gate, with /features/payments retained as an alias. Module lifecycle changes pass through the control-plane API and Unit of Work. Disable retains tenant assignments for re-enabling; Retire permanently withdraws access without deleting domain data. Core and IAM remain essential.
