# UI-to-Backend Flow

The default product-building flow is:

1. Start from the UI journey.
2. Call backend APIs through typed HTTP clients.
3. Keep DTO contracts in `ST.SharedKernel`.
4. Keep UI state, presentation models, and browser/session concerns in the UI layer.
5. Push business rules into Application and Domain when the journey exposes them.

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