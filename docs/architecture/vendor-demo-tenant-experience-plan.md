# Vendor, demo and tenant experience delivery plan

Status: approved and in active implementation. Delivered slices and current verification are recorded below; remaining acceptance gates stay open until verified.
Date: 2026-10-29

## Objective and delivery order

Deliver a standard, administrator-configurable SaaS platform with explicit Platform Admin, Tenant Admin and Tenant Support Session contexts. A vendor-owned demo tenant is mandatory. Deliver vendor capabilities first, enter the demo tenant early, then onboard subscribed/customer tenants through the same application paths. Verify tenant boundaries in every phase rather than adding isolation after the vendor UI is complete.

Retain the existing .NET 10 modular monolith, IAM/Shared/ControlPlane ownership, API boundary, Interactive Server Blazor and hybrid deployment-stamp strategy. Loans and Members are explanatory downstream examples, not business modules to introduce into StarterTemplate.

## Evidence and gaps in the current implementation

- ControlPlane already stores tenants, stamp assignments and module-key assignments. TenantModulesDialog currently edits free-text keys, not registered module definitions.
- MenuRoute is a database-backed catalogue seeded by IamReferenceDataSeed. Registration does not discover or implement pages.
- MenuItem already has parent, placement, module and role/permission visibility fields. New visibility collections and both provider migrations exist; the new SQL Server migration was applied locally. Parent filtering currently permits visible orphans to become roots.
- The ControlPlane Blazor layout requires an explicit control_plane.manage claim while the API permission handler has a broad System Administrator bypass. Platform identity needs an explicit boundary; assigning this permission to all tenant admins is not the solution.
- AppRole currently has no explicit TenantId. Role ownership, name uniqueness and Identity store assumptions must be resolved before claiming tenant-private roles.
- Feature flags currently resolve configuration values, not a tenant override hierarchy.
- The current support start command creates an audit record. That alone does not prove tenant context switching, consent enforcement, expiry enforcement or safe credential lifecycle. Audit the entire path before enabling support entry.

## Contexts and authority

| Context | Authority | Explicit exclusions |
| --- | --- | --- |
| Platform Admin | Global module/page/permission definitions, navigation defaults, role templates, tenant onboarding/availability, flags and support policy | No implicit unrestricted customer data access |
| Tenant Admin | Current tenant navigation overrides, users, roles, assignments and permitted tenant configuration | Cannot activate unentitled modules, modify platform roles or grant platform permissions |
| Tenant Support Session | Approved target tenant, approved scope and time window, with original vendor actor preserved | No ambient platform authority inside tenant operations; no arbitrary tenant ID switching |

Demo is a tenant classification, not a fourth privilege context. Vendor users enter it through explicit membership/context selection and use normal tenant rules. Platform Admin remains accessible separately. Context switching must be server-authorized, refresh claims/state, namespace caches by tenant/context and clear stale UI, protected token and session state. Expired support access must fail at the API, not merely hide a banner.

## Administrator ownership and implementation status

Authorized platform administrators can create modules, page registrations, permission definitions, role templates and default navigation. Developers provide guidance, implement code and wire declared permission/module checks. Tenant administrators may create tenant-owned definitions/overrides within enabled modules and permitted extension points.

Distinguish configured availability from deployment evidence: Draft, Awaiting implementation, Available and Retired are proposed user labels. An admin cannot make missing executable functionality work merely by marking a record available; destination/operation bindings must be verified against the deployed implementation. Do not execute user-supplied handlers, Razor or code from catalogue records. Keep stable technical identifiers internally and expose friendly labels.

## Ownership and effective hierarchy

- ControlPlane owns platform module definitions, tenant entitlements, tenant classification and lifecycle, and support grants.
- IAM owns permissions, role templates/tenant role assignments and navigation. Shared contracts connect contexts; avoid cross-context entity navigation or cross-stamp database joins.
- Module -> pages/navigation groups/items/permissions/default role templates. Navigation keeps Parent as the field name. A tenant-defined role may combine permissions from several enabled modules.
- Standard definitions plus tenant overrides; preserve stable IDs and provenance. Do not clone every global record for every tenant. Specify global-definition retirement and override conflict behavior before migrations.
- Tenant availability = deployed capability + active module entitlement + satisfied dependency/ancestor availability + applicable feature flags. User permissions/role restrictions further narrow access. No flag grants access or bypasses an entitlement.
- Disabling/expiry hides descendant navigation, pages and module-specific assignable permissions/templates and rejects direct API calls. Existing business data and assignments remain; reenabling restores valid configuration.
- Cross-module roles remain visible when they retain an available permission or administrative purpose. Their unavailable module permissions are ineffective and not assignable. Global account/security capabilities remain clearly separate from optional business modules.
- A hidden ancestor suppresses its descendants; do not reparent them silently. Reject cycles, incompatible module parents and invalid hierarchy changes. Define safe parent moves and module dependency removal.
- Effective-policy changes invalidate tenant module, navigation, permission and flag caches and relevant active session state, including workers and long-lived Blazor circuits. Test the chosen propagation guarantee explicitly.
- Replicate/version the required catalogue and effective policy into deployment stamps through the existing control-plane/data-plane boundary. Do not assume a pooled database or process-local cache for all deployments.

## Required demo tenant

- Explicit vendor-owned Demo classification and stable identity; distinct from bootstrap administrator identity and any paying customer.
- All deployed, implemented tenant-facing modules enabled. Platform-only controls remain in Platform Admin. Unimplemented registrations are shown in the platform catalogue as unavailable and do not become broken demo links.
- The demo uses the same provisioning, tenant resolution, navigation, permissions and API paths as customer tenants. No global showcase switch that exposes demo features to customer tenants.
- Vendor-only demo access by default, optional separately authorized sales/demo viewers later. Provide normal Tenant Admin plus selected role previews through actual scoped identities or sessions, not client-only role simulation.
- Visible Demo context marker, deterministic sample data, versioned module fixture hooks and an idempotent refresh process. Reset must prove the Demo classification and exact target, require appropriate authorization and record its actor. Never target arbitrary tenant IDs.
- Sandbox/capture adapters for payment, email, SMS and other external side effects. No copied production credentials, personal data, real charges or uncontrolled deliveries. Configuration must fail safely if a production adapter is selected for the demo.
- New implemented modules become eligible for demo enablement after their policy/fixture/adapter checks pass. No implicit production entitlement changes.

## Phases and acceptance gates

### Phase 0: establish identity and migration baseline

Tasks: audit current bootstrap account, role/permission claims, tenant resolver, Identity role ownership, existing support flow and data. Record the source of the current user's missing ControlPanel access. Define explicit platform staff membership, tenant memberships, role uniqueness, reserved platform permissions, actor/tenant context contracts, and compatibility for existing accounts/roles/menu keys. Inventory current database and migrations without resetting data.

Gate: a reviewed role/context matrix and concrete migration/backfill decisions exist; vendor staff and tenant admins cannot gain each other's authority through role names, edited claims, catalogues or arbitrary tenant IDs. No automatic conversion of every System Administrator to vendor staff.

### Phase 1: vendor workspace and module catalogue

Tasks: consistent Platform Admin authorization in API and UI; a verified vendor bootstrap path with no committed credentials; Modules list/detail with status, module dependencies, Pages, Navigation, Permissions and Role templates. Platform admins manage definitions; page/operation implementation remains code-backed. Tenants list shows classification, lifecycle and enabled module selection from the catalogue. Add clear context entry and exit affordances.

Gate: authorized vendor sees the complete platform catalogue; tenant admin cannot enter platform screens or invoke platform APIs. Known current account access is verified through the real login/MFA flow. No arbitrary module-key assignment or broken registered page activation.

### Phase 2: vendor-owned demo workspace

Tasks: create/provision Demo through the shared onboarding path; enable all ready tenant-facing modules; seed safe examples; enter Demo from the platform header with normal scoped tenant membership. Build Demo/reset management and show readiness of module fixtures/integrations. Provide role-specific demo access for testing restrictions.

Gate: vendor can enter Demo, view the complete implemented application, exercise allowed flows and return to Platform Admin. Data, tokens/caches, uploads, notifications and external side effects are isolated. Repeated setup does not duplicate records; a reset cannot affect a non-demo tenant. Role-restricted views really reject forbidden API operations.

### Phase 3: shared hierarchical availability and tenant customization

Tasks: connect module/page/menu/permission/template ownership; tenant-aware flags with explicit global/default/tenant precedence; effective entitlement and authorization checks on API and navigation. Implement subtree suppression, dependency checks, tenant overrides, tenant-private roles and active-session/cache invalidation. Rename Menus to Navigation, Create Menu to Add navigation item, Placement to Show in; retain Parent. Rename Features to Modules only once it renders the genuine module catalogue.

Gate: an enabled/disabled module matrix passes in Demo and at least two isolated test tenants. Disabled modules disappear from navigation and assignable access catalogues, and direct URLs/APIs cannot bypass policy. Cross-module roles retain only effective access. Tenant overrides do not modify global defaults or another tenant. Revocation is verified in an already-open session/circuit.

### Phase 4: subscribed/onboarded tenant experience

Tasks: tenant request/create -> approval -> entitlement selection -> provisioning -> administrator invitation -> activation. Use versioned defaults and the same provisioning pipeline proven by Demo. A normal Tenant Admin sees only entitled modules and their own configuration. Use existing subscription metadata/administrative entitlement assignment first; paid checkout and automated billing integration are separate work unless explicitly included later.

Gate: vendor onboards a second normal tenant from the UI; invited admin signs in, completes required MFA, customizes navigation/roles, and sees only that tenant's modules. Demo-only features/sample records do not appear. Suspension, module expiry, retry and reenabling behave predictably without data loss. Pooled isolation tests and existing stamp contracts remain valid; isolated deployment claims require their own actual evidence.

### Phase 5: managed tenant support

Tasks: reason, tenant approval or explicit approved support policy, granted scope and duration; preserve vendor actor and selected tenant; audited entry/actions/exit, visible support banner; read-only default and explicit write scope. Enforce expiry, cancellation, tenant suspension and revocation at API/session boundaries. Hide/disable incomplete support actions until this phase is accepted.

Gate: vendor cannot enter customer data without a valid grant; support can be revoked by tenant/vendor, expires server-side, does not inherit platform bypass permissions, and exits cleanly to Platform Admin. Tenant-private configuration is visible only to authorized tenant users and active scoped support. Audit records alone do not count as working support sessions.

## UI walkthroughs required for acceptance

1. Platform Admin -> Modules -> module -> Pages / Navigation / Permissions / Role templates.
2. Platform Admin -> Demo -> enter -> Tenant Admin -> module pages -> role-restricted view -> return to platform.
3. Platform Admin -> Tenants -> select modules -> provision/invite -> customer admin login -> own Navigation and Access management.
4. Disable a module while a tenant user has a page open; verify navigation, catalogues, API denial and eventual reenabling.
5. Tenant approval -> vendor support entry -> expiry/revocation -> exit; demonstrate the same denial through direct API calls.

Compact MudBlazor UI, searchable selectors, plain labels and visible context identity throughout. Avoid technical keys, implementation prose and decorative placeholder dashboards in normal user flows. Expose implementation readiness only where admins need it to configure a working capability.

## Verification and delivery discipline

For each phase: focused domain/application tests, provider persistence coverage where changed, API authorization and cross-tenant tests, cache/session behavior and signed-in browser walkthrough on desktop/mobile widths. Generate and review separate SQL Server/PostgreSQL migrations, preserve unrelated work and existing data, update DI fixtures as needed and run required API/architecture checks. Existing running Debug hosts can hold assemblies; use Release verification without stopping the user's processes.

Use real handlers and HTTP boundary tests where feasible. Demo is a working reference, not proof of paid provider, production or every stamp certification. Report exactly what was exercised. Deliver one bounded phase at a time; do not claim an incomplete phase ready based on compilation alone.

## First implementation slice after review

Begin with Phase 0 and the smallest Phase 1 vendor entry: diagnose the current account's access, make the platform boundary consistent, and provide an honest Modules catalogue screen. This must be followed by the Demo tenant slice before expanding into broad catalogue editing or customer-specific configuration. This plan does not authorize changing credentials, granting every administrator vendor access, publishing, billing activation or production deployment.

## Working vendor/demo slice (2026-10-29)

Implemented: explicit platform-staff plus reserved permission authorization (tenant System Administrator is not platform staff); persisted module catalogue and code-backed availability; catalogue-based tenant module assignment; vendor-owned reserved demo tenant with tenant-administrator and read-only sessions; per-tab demo token storage and return to platform; live vendor revocation checks; tenant-owned role uniqueness and catalogue keys. Arbitrary module definitions remain unavailable until their implementation is registered in code. Core and IAM cannot be globally disabled. Managed customer support entry is disabled until scoped support is implemented.

The current local bootstrap account is opted in through DevelopmentSeed:GrantPlatformAccess in user-secrets, not a committed credential or automatic promotion of every administrator. Existing password/MFA remain unchanged. Sign out/in after restarting an existing Visual Studio host to receive updated claims.

Verification: API and Blazor Release builds passed; 156 unit and 73 architecture tests passed. Real JWT integration exercised catalogue creation/readiness, platform rejection for nonstaff, demo identity separation, navigation module disable/reenable, forbidden live payments, repeat entry, read-only writes, refresh, and vendor revocation. Fresh migration plus repeat application passed for SQL Server and PostgreSQL. Local ST database on localhost:14335 received VendorWorkspaces and TenantCatalogueScope; no production migration was run.

Preview hosts: API http://localhost:5291 and Blazor http://localhost:5292. These are separate Release verification processes; normal Visual Studio profiles remain unchanged. Preview messaging is disabled through the existing Messaging:Enabled process override because installed MassTransit requires a license; messaging is not certified by this walkthrough. Signed-in browser verification completed after the user performed normal login/MFA: module catalogue, tenant administrator demo, hard reload inside demo, tenant administration, return to original vendor identity, read-only demo and return. This exposed and fixed a hard-reload authentication initialization failure. Broader customer/onboarding/support acceptance remains pending.

Not yet complete: module Pages/Navigation/Permissions/Role-template detail tabs; dependency-aware hierarchical permission and role filtering; tenant-specific feature flags and overrides; shared demo/customer onboarding orchestration, complete sample fixtures and demo reset; subscribed customer onboarding; managed support grants/sessions. This slice does not complete all Phase 1/2 acceptance gates.

Walkthrough: sign in -> Platform administration -> Modules; open Tenants to assign ready modules; open Demo -> select Tenant administrator or Read-only user -> Enter demo -> verify context banner -> Return to platform. Demo creation occurs on first entry. Outbound payment/invitation operations are blocked until sandbox adapters are certified.

## Control Panel listings and module lifecycle (2026-10-29)

Payments is now a separate implemented module with its own entitlement and API gate. The default workspace receives Payments; demo entry enables all active, implemented modules including Payments. Standard Features navigation is upgraded to a Payments entry at /payments, including uncustomized copied demo/tenant menus. /features/payments remains an alias. Existing custom tenants need an explicit Payments assignment; a Showcase subscription no longer grants payment access.

Control Panel now hosts Deployment Stamps, Tenants and Modules in URL-addressable tabs (?tab=stamps|tenants|modules), reusing the same table components as their existing direct routes. Separate Modules/Tenants sidebar shortcuts were removed. Dashboard comes first, followed by workspace modules and an administration section. Demo workspace and Solution Overview sit above the bottom inset. Admin Center and the legacy Features hub also use tables instead of navigation cards.

Modules use the common row icon actions, search and pagination. Disable/Enable is reversible; Retire is terminal, preserves business data and withdraws module access for all tenants. Retired modules cannot be edited, enabled or newly assigned. Core and IAM cannot be globally disabled or retired. Lifecycle changes require confirmation and refresh the current UI navigation. Disabled assignments remain persisted for later re-enabling.

Verification: API and Blazor Release builds, 159 unit tests, 73 architecture tests, the real-session vendor/demo lifecycle test, and both fresh-database migration/rerun suites passed. The lifecycle integration verifies Payments API/navigation denial, Showcase independence, terminal retirement and essential-module protection. Provider-specific PaymentsModuleLifecycle migrations were generated and reviewed; local SQL Server ST on localhost:14335 was upgraded. The preview retains Messaging:Enabled=false for its existing MassTransit licensing limitation.

Signed-in browser verification also passed for all three tabs, grouped/pinned sidebar placement, and disabling/re-enabling Payments through the confirmation dialogs. The sidebar hid/restored Payments immediately after each lifecycle update; Payments was left active. Retirement was verified in the disposable integration fixture rather than applied to a live local module.


## Current completion slice (2026-10-30)

The module catalogue and vendor/demo/customer hierarchy are implemented. The current acceptance record is [vendor-demo-tenant-completion-checklist.md](vendor-demo-tenant-completion-checklist.md); earlier dated sections above remain historical checkpoints.

The hierarchy is Module -> Page / Permission / Role template / Flag. Platform administrators own standard definitions and navigation defaults. Tenant administrators receive only entitled, active modules whose dependencies are available; they can apply templates and save tenant-owned navigation/audience overrides. A menu never grants page or API authority. Disabled-module assignments remain dormant rather than being deleted, so reenabling preserves tenant configuration. Required page permissions remain effective even if optional audience restrictions are changed.

The Control Panel module details contain Pages, Navigation, Permissions, Role templates, Flags and Dependencies. A deployed code manifest is authoritative for executable modules/pages; creating a definition prepares configuration and does not implement a function. Global flag defaults can be overridden for a particular tenant without granting an unentitled module. Navigation overrides carry versions and support reset to platform defaults, including legacy copied menus.

Customer flow: Control Panel -> Tenants -> Create -> Approve -> Manage modules -> Set up workspace -> Send invitation. Setup is retryable, uses the same initializer as Demo, and records invitation, email-verification and MFA status. Invitations and recovery links identify their workspace; normal sign-in resolves its workspace before credentials. User-name uniqueness and protected browser storage are tenant-scoped. Initial Tenant Administrator access requires MFA; vendor authority is never part of this tenant role.

Support flow: vendor selects a customer in Tenants -> Request support; requests a reason, permissions, duration and read-only/write mode. The customer reviews the request in Admin Center -> Support access and can narrow or revoke it. Approved entry creates a scoped tenant identity and keeps the original vendor audit actor. Every customer request checks grant validity, current vendor authority, tenant status and approved scope. Writes require explicit approval and record audit intent before dispatch and status afterward. Scope is enforced independently of mutable proxy roles/claims. Expiry, revocation and exit deny the API and token refresh. The banner identifies the customer, expiry and access mode, with return to the saved platform session; expired sessions retain a return action on sign-in. Activity is visible only to the authorized vendor or approving tenant.

Demo entry uses global definitions rather than copying the vendor's catalogue. Reset revokes demo sessions and runs versioned fixture hooks against the reserved demo only. Existing customer navigation is unaffected. Outbound demo payment and invitation operations remain blocked until certified adapters exist.

SQL Server and PostgreSQL migrations add catalogue/overrides, workspace setup state, support grants/audit and tenant-scoped user-name uniqueness. The verified local target is ST at localhost:14335. No production migration or cloud dispatch was performed. ControlPlane:Provisioning:Enabled defaults false; an unconfigured isolated dispatch fails explicitly and can be retried instead of pretending to provision infrastructure.


## Local delivery acceptance (2026-10-30)

The agreed local implementation is delivered. Final verification: API/Blazor Release builds; 162 unit, 73 architecture and 28 integration tests passed; the existing RabbitMQ transport test remains explicitly skipped. Fresh SQL Server/PostgreSQL migration and repeat application are included. Local SQL Server ST has the provider-specific upgrade migrations. Patch whitespace passed; changes remain uncommitted.

Signed-in browser checks verified module tabs, the corrected navigation defaults and Parent behavior, Demo entry/reload/return, customer create/approve/entitlements/setup status, support request views and mobile navigation/layout. The empty local Preview customer is ready for its initial administrator invitation; no invitation was sent during the manual walkthrough. Customer invitation/MFA and approved support actions/exit/expiry/revocation were verified through real HTTP/JWT/Mailpit tests. A separate manual customer credential ceremony, provider sign-in/live payments, messaging transport and actual isolated cloud rollout are outside this verification.

Late walkthrough regressions are covered: existing tenants receive newly deployed modules through explicit inserts; operator tenant reads immediately reflect approvals and entitlement edits; root navigation defaults do not accidentally match a legacy item with a null catalogue key. Technical exception text is suppressed in product messages in Development as well as Production.
