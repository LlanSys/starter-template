# Tenant identity, branding and sign-in

## Tenant and workspace

For a customer in StarterTemplate, workspace is the operating context of that tenant company. There is no separate customer workspace hierarchy to configure: the selected workspace resolves to the tenant ID. The vendor platform (default) and vendor Demo are special operating contexts in the existing model.

Tenant context determines which company's accounts are searched, which company issues the sign-in challenge, which deployment stamp/database serves the request, and which tenant scope is applied to data and caching. It is separate from authorization: credentials and MFA prove the account, and live operation permissions determine allowed actions. A company code is not a password or permission. Editing it does not move accounts or grant access. Matching email addresses can represent independent identities in two tenants.

## What a person should see

A customer should normally follow the company's sign-in/invitation link. The app resolves the tenant before displaying credentials and brands the card with that tenant's registered display name and derived initials. A company-specific address should resolve the tenant through trusted routing. It should not require ordinary employees to memorize a technical workspace ID.

A shared entry address with no organization information cannot identify a tenant unambiguously. Keep a fallback organization-code step, not a public directory of customers or an email-based account-linking shortcut. The user-facing controls are Organization Code and Change Organization. Internal routes, contracts and protected storage retain workspace terminology for compatibility.

Current local examples:

- /iam/sign-in?workspace=qa-alder-01 resolves Alderfield QA Services, badge AQS.
- /iam/sign-in?workspace=qa-brook-02 resolves Brookmere QA Supplies, badge BQS.
- /iam/sign-in?workspace=default is the vendor platform, using the product brand StarterTemplate/ST.

A shared URL uses the remembered/default selection and the Organization Code fallback. Company-specific frontend discovery is implemented: the Blazor host resolves its actual browser origin before restoring a session, and the API binds a registered company origin to exactly one active tenant on the current stamp. A conflicting query, selector or account session is rejected. Unknown, disabled and wrong-stamp company origins fail closed; they do not fall back to another company.

## Company app addresses

A platform operator with `control_plane.edit_tenants` opens Control Panel > Tenants > row menu > App Address. Enter one canonical company origin, such as `https://app.customer.example`, and save. Viewing the mapping requires `control_plane.view_tenants`; changing it requires the edit operation. Revisions reject stale edits and a database unique index prevents assigning one origin to two tenants, including concurrent registrations. Clearing this field removes the dedicated binding and leaves the shared portal available. Existing provisioning `ClientOrigin` remains a separate legacy/shared value and cannot overwrite the dedicated mapping.

The local Docker QA mappings are `http://alder.localhost:8181` and `http://brook.localhost:8181`. Chromium resolves `.localhost` to loopback; no Windows hosts-file changes are required. The same running Blazor container serves both addresses. No organization code or Change Organization control appears on those company-bound sign-in pages. Opening a different company's query link on the bound address is rejected. The platform/shared entry remains `http://localhost:8181/iam/sign-in?workspace=default`.

Deployment/configuration checklist:

- Apply the ControlPlane migration for the selected database provider. Both SQL Server and PostgreSQL migrations are included.
- Set API `Tenant:CentralFrontendOrigins` to the explicit shared/vendor frontend origins. Local defaults cover Docker `http://localhost:8181`, Visual Studio HTTP `http://localhost:5206` and HTTPS `https://localhost:7149`. Do not register these shared addresses as a company's dedicated address. Environment configuration uses `Tenant__CentralFrontendOrigins__0`, etc.
- Register each company's unique frontend origin through App Address. Production origins must be HTTPS. Plain HTTP is accepted only for loopback/`.localhost` development. Paths, credentials, query strings and fragments are rejected.
- For real domains, configure DNS, TLS, ingress/host forwarding and the Blazor host's allowed hosts to serve that origin. This feature configures application routing; it does not create DNS records or certificates. Keep API `CentralApiHosts` and trusted proxies explicit too.
- Set `BackendApi:BaseUrl` to the actual private API endpoint and `PublicBaseUrl`/`ExternalSignIn:PublicApiOrigin` to the actual browser-accessible API. A company frontend address never supplies the API base URL.
- The UI supplies `X-Frontend-Origin` from the browser address on JSON, upload and download calls. This is routing metadata, never identity proof; live JWT, tenant, MFA and operation-permission checks still apply. Direct API clients may omit this UI header and retain the existing signed-tenant boundary.
- SSO permits a registered active tenant's company return origin only for that same tenant and the exact `/iam/sso-callback` route. Provider callback URLs remain on the configured public API origin (`/signin-google`, `/signin-oidc`); company frontend aliases do not require one provider callback per customer. Shared return origins stay explicitly allowlisted in `ExternalSignIn:ClientOrigins`.
- Keep pre-activation KYC submission links on the shared/vendor collection portal. Company runtime access still requires KYC approval and completed provisioning. A company address grants no invitation, evidence or account access.
- Verify both customers, mismatched query/header/session rejection, unknown/suspended addresses, fresh password/MFA, uploads/downloads and SSO return validation after deployment. Fresh real Google/Microsoft provider sign-in remains in Allan Run as agreed.

A shared app address is useful for vendor/platform operations, a central SaaS sign-in/intake portal, people who work across companies, and local diagnostics. It does not require companies to share login emails, passwords or accounts.

## Branding behavior

The source of company identity is the existing tenant display name, not a duplicate organization setting. Initials are a fallback visual mark until company logos are implemented. The same registered company identity appears in the sign-in card, subsequent auth pages and authenticated sidebar. The header describes the current page; Profile calls the field Organization.

OrganizationBranding.Initials takes up to three word initials, retaining grapheme elements, ignoring punctuation separators and normalizing display case. A single CamelCase/acronym word uses up to three capital initials; an ordinary single word uses its first text element. Examples: Alderfield QA Services -> AQS, Brookmere QA Supplies -> BQS, StarterTemplate -> ST, Stripe -> S. Empty/non-letter-only names use a neutral question mark. No hardcoded initials for QA tenants or user-supplied SVG are used.

The initial anonymous/loading shell has a neutral brand placeholder. Sign-in waits for resolver/provider initialization before rendering its resolved company brand and controls, preventing a remembered company's brand from appearing while another requested company resolves. Invalid selections show the organization-code fallback. Explicit tenant invitation/recovery pages only reuse selected branding when the target tenant matches. Client KYC submissions use the company name returned by the validated submission inspection, without adopting another signed-in company's branding.

WorkspaceSelection keeps branding metadata circuit-scoped and emits changes only when the resolved selection actually changes. Sidebar identity must match the signed-in tenant; platform/support/Demo policies and protected session switching are preserved. IsPlatformWorkspace in the public routing DTO is display metadata only. It is never accepted as a platform identity or authorization grant.

## Verification and boundaries

Run the focused initials tests and public resolver metadata/ingress regression tests; build API/Blazor and check source formatting. Local browser acceptance should exercise both customer links, a failed selection, switching organizations with cleared credentials, a real password/MFA sign-in and the matching sidebar/Profile identity. Keep Google/Microsoft fresh-provider sign-in in Allan Run as already agreed. No logo upload, dynamic favicon, DNS/TLS provisioning or new IAM model is part of this change.

## Earlier branding-only acceptance - 15 November 2026

Implemented and exercised in the existing local Docker deployment. Unit suite: 266 passed. Architecture suite: 74 passed. Focused integration: three passed (new public routing display metadata plus existing ingress boundary tests). All three runs had zero failures/skips. API and Blazor Linux images built; analyzer warnings remain. Focused formatting and 19 preservation fixtures passed. Local endpoint audit passed with 76 allocations/nine independent secret stores. No migrations or secrets changed.

Browser observations: the initial shell was neutral; Alderfield showed AQS/name in the auth brand area without a Signing In To line; switching to an unavailable code kept credentials hidden; switching to Brookmere showed BQS/name and cleared the draft username. The default vendor entry used ST/StarterTemplate. A tenant-scoped Alder link selected the company without manual code entry. Real synthetic administrator password/MFA sign-in retained company branding on the verification step and sidebar. Profile displayed Organization with Alderfield QA Services, and the dashboard account summary is named Your Account. The header shows page context instead of repeating the company name.

Screenshots are in output/tenant-branding/st-alder-branding.jpg, st-brook-branding.jpg, st-platform-branding.jpg and st-signed-in-branding.jpg. TRX evidence is tenant-branding-unit.trx, tenant-branding-architecture.trx and tenant-branding-integration.trx under the corresponding test project's TestResults folder. These are local acceptance results, not a full device/browser or cloud-domain certification. Fresh Google/Microsoft sign-in remains in Allan Run as previously agreed.

Company branding never changes DataProtection:ApplicationName, signing keys, issuer/audience, tenant IDs or permission grants. No accounts, passwords or MFA enrollments were changed. The existing working tree remains uncommitted; no push or cloud deployment was performed.

## Discovery budgets and passkey domains

Public workspace resolution uses its own configurable `RateLimiting:WorkspaceDiscoveryPolicy` (120 reads/minute per existing IP partition by default, no queue). It no longer consumes the five-per-minute authentication-operation budget. Credential login, MFA, recovery and linking limits are unchanged. Excess discovery requests receive 429 immediately instead of waiting beyond the UI timeout. Scale the discovery budget with expected shared frontend traffic.

Passkeys remain bound to their configured WebAuthn relying-party domain and allowed origins. Registering a company frontend address does not migrate existing authenticator credentials to a different domain. Sign-in keeps the Passkey action in its fixed position but disables it for a mismatched relying-party domain; automatic conditional prompts are skipped there. Use password or connected Google/Microsoft sign-in with app MFA at company aliases. Configure and independently verify passkeys at the actual authentication origin before enabling them in a downstream deployment.

The subsequent hostname routing, distinct-person QA account changes and executed acceptance results are recorded in [company address verification](frontend-host-routing-verification-2026-11-15.md). That follow-up supersedes the earlier hostname-discovery limitation and current-account snapshot.
