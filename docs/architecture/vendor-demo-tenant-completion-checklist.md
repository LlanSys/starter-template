# Vendor / demo / tenant completion checklist

## Local IAM and tenancy hardening — 2026-11-09

- Replaced administrator role-name shortcuts with current tenant-scoped grants. Protected reserved roles and checked role and direct-grant delegation through the IAM mutation paths.
- Rebuilt bearer authority from current account, role, module, and permission state. Bound access to signed tenant and session claims. Reactivation persists a cutoff that prevents earlier sessions and refresh tokens from returning.
- Restricted forwarded headers to configured proxies and validated workspace hosts, selectors, and signed claims. Reloaded authoritative routing state and required a suspended workspace for database-route changes.
- Enforced tenant ownership on tracked and detached writes, including Identity relationships, for SQL Server and PostgreSQL. Scoped queued jobs and set-based credential cleanup.
- Added the session cutoff, provisioning attempt/deadline, client origin, exclusive stamp reservation and workspace setup lease migrations for both providers. Fresh and upgrade migration tests passed, including repeat application.
- Completed the isolated provisioning workflow source with commit-tagged API and UI images, infrastructure, destination migrations, runtime configuration, health checks and GitHub OIDC completion/failure reporting.
- Bound dedicated hosts and invitation workers to their owning stamp. Verified exclusive database ownership, current-attempt completion and idempotent callback handling.
- Added a persisted setup lease and deferred local invitation delivery until role initialization completes. The final suite covers concurrent HTTP setup and stale-owner fencing.
- Verified tenant-only IAM account movement, suspended cutover, physical routing and rollback on both providers. Client origin moves with the database route, and public workspace discovery can recover from a stale browser selection.

### Verification

- Solution and Docker builds passed with compiler warnings. All 171 unit and 74 architecture tests passed.
- The final full integration suite passed 52 tests and skipped the opt-in RabbitMQ transport test. The transport test passed separately during the preceding hardening pass.
- The corrected concurrent setup and original invitation/MFA/support cases also passed together in a focused run. The initialization race found during verification was fixed before the final suite.
- Bicep compiled with warnings, actionlint passed, and the provisioning/report scripts passed syntax and simulated-provider checks. Their Azure execution remains pending.
- The final disposable renamed downstream source export built successfully and passed 74 architecture tests with fresh user-secret identities. It excluded ignored local configuration.
- Focused whitespace formatting for new boundary code and regression tests, final patch whitespace, and the local endpoint allocation audit passed.
- Rebuilt and started the local Docker API and Blazor hosts. The ST database on localhost,14335 has zero pending migrations across ControlPlane, IAM, and Shared.
- Live Docker HTTP probes passed API health, the Blazor sign-in shell, public workspace discovery, unauthenticated machine callback and GraphQL denial, unknown hosts, conflicting workspace selectors, and untrusted forwarded headers.
- Google and Microsoft challenges redirected to their provider hosts. Unapproved return origins and callbacks without verified external state were rejected. Completing sign-in with real provider accounts remains unverified.

### Remaining release gates

The user selected local Docker for this pass and kept cloud sign-off pending. Provisioning, completion/failure reporting and cutover/rollback implementation are delivered and verified within the local test scope above. Cloud execution must verify the deployment identity and Key Vault/database access, ingress, actual provider-account sign-in, and restoration of the deployment's data, key ring, private assets and queued work. Record measured recovery objectives from that rehearsal.

The local Docker baseline includes these changes. Restart any Visual Studio API process that predates this baseline. Release publication and CI results are tracked in the repository PR history.

## Historical verification — 2026-10-30


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
