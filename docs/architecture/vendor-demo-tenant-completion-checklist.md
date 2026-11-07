# Vendor / demo / tenant completion checklist

Local implementation delivered and verified on 2026-10-30 in E:/Repos/StarterTemplate. The existing working tree is preserved; changes remain uncommitted. Source delivery and verification boundaries are recorded separately.

## Implemented

- [x] Central module catalogue: implemented pages, required permissions, navigation defaults, role templates and definition versions.
- [x] Module dependency hierarchy, global availability, tenant entitlements, global flag defaults and tenant overrides.
- [x] Effective availability shared by API authorization, permission and role catalogues, navigation and native Blazor page policies.
- [x] Tenant navigation overrides, searchable role/permission selectors, page-loaded defaults, Position and read-only Module, reset to platform defaults, cycle/ancestor protection.
- [x] Private tenant permission ownership, module-aware roles, role-template application and preservation of dormant assignments.
- [x] Vendor module detail tabs: Pages, Navigation, Permissions, Role templates, Flags and Dependencies.
- [x] Shared demo/customer workspace initialization without catalogue copying.
- [x] Reserved Demo workspace, active implemented modules, versioned fixture hooks, guarded reset and session revocation.
- [x] Customer approval, entitlement selection, workspace initialization, administrator invitation, normal acceptance/login/MFA, status and retry.
- [x] Tenant-aware anonymous sign-in/recovery/invitation routing and protected per-tab workspace selection, with tenant-namespaced normal token storage.
- [x] Managed support requests, customer approval/narrowing, scope, read-only default, duration, proxy identity, audit intent/results, live vendor checks, revocation/expiry and explicit exit.
- [x] Vendor and tenant support listings/activity, visible support context and return to platform, including recovery after expiry.
- [x] SQL Server and PostgreSQL migrations; local SQL Server ST database upgraded.

## Verification

- [x] API and Blazor Release builds.
- [x] 162 unit tests.
- [x] 73 architecture tests, including both providers' pending-model checks.
- [x] Final full integration suite: 28 passed, one existing RabbitMQ transport test explicitly skipped. This includes fresh database migration and repeat application for SQL Server and PostgreSQL.
- [x] Normal customer invitation through Mailpit, acceptance, password sign-in, MFA enrollment, same email in two tenants and idempotent setup.
- [x] Two-tenant module/flag/role/navigation matrix, missing/hidden ancestors, cycle rejection, stale override versions, reset and direct API denial.
- [x] Read-only support, wrong-tenant approval rejection, scope enforcement even with altered proxy claims, refresh denial after revocation and server-side expiry.
- [x] Demo reset preserves customer navigation and revokes old demo sessions.
- [x] Existing Demo missing a newly deployed module, explicit support writes/create/update audit actor, clean exit through the v1.0 route alias, audit isolation and live vendor revocation.
- [x] Signed-in browser: vendor listings/module tabs; Demo administrator entry, hard reload and return; corrected navigation page defaults with root Parent, Module, Position, icon and permission; customer creation, approval, entitlements and setup status; support request form at desktop and 390px, mobile navigation; tenant-specific sign-in routing.
- [x] Final API/Blazor builds, architecture/provider models, unit/integration checks, patch whitespace and plan synchronization.

## Verification boundaries

The preview runs API 5291 / Blazor 5292 with Messaging:Enabled=false because the installed MassTransit runtime requires licensing. The existing RabbitMQ transport certification remains separate. No production deployment, paid billing, external payment execution or Google/Microsoft provider certification is claimed. Isolated cloud provisioning is disabled by default; its dispatch/callback contracts remain, with explicit failure and retry rather than simulated readiness.

The starter fixture owns navigation configuration only. Business modules add their own IDemoFixture implementations; the template does not fabricate Loans/Members data. A reset accepts no arbitrary customer target and only operates on the reserved Demo identity.

The final browser check found that an existing tenant's newly added module assignment was inferred as an update because its application-assigned Guid was already populated. Writes now explicitly add the assignment through the ControlPlane Unit of Work repository. A regression test starts with an existing Demo missing Payments.


## Final preview and manual verification scope

Preview API: http://localhost:5291; Blazor: http://localhost:5292. Current owned processes are recorded outside the repository in C:/Users/PC/AppData/Local/Temp/starter-completion-preview-pids.json. Do not stop unrelated Visual Studio hosts.

The local **Preview customer** workspace (identifier preview-customer, hostname preview-customer.invalid) is approved on the pooled stamp with Core, IAM and Payments. Its initial administrator is deliberately not created and its invitation has not been sent. Sign in to workspace default, then Control Panel -> Tenants -> Preview customer -> Set up workspace to inspect the remaining setup steps.

Manual signed-in checks covered the vendor, Demo, customer configuration and support-request views. Customer invitation acceptance/MFA and customer-approved support writes, revocation, expiry and exit were exercised through real HTTP/JWT/Mailpit integration tests; a second manual customer credential ceremony was not performed. The preview session timed out at the end of the walkthrough and is left at normal sign-in with default selected. No authentication checks were bypassed.

The walkthrough also found and fixed a stale tenant-list/detail cache: operator configuration reads now bypass cached results, and an HTTP regression proves immediate visibility after create, approve, module assignment and removal. Confirmation copy is compact and distinguishes pooled readiness from failed/requested provisioning; mandatory Core/IAM assignments are labelled Required.

Final evidence: 162 unit tests; 73 architecture tests; 28 integration tests passed, 1 existing transport test skipped. Both fresh-provider migration/rerun tests are included. The last additional onboarding/support suite passed 3/3. API/Blazor builds succeeded; existing compiler warnings remain. git diff --check passed. No production migration/deployment, commit, push or PR was performed.
