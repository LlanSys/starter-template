# StarterTemplate domain neutral refactor plan

Date: 2026-10-06
Status: Phase 0 complete. Phase 1 bootstrap and independent account creation/editing implemented; remaining account/auth state and business removal are pending.

## Objective and repository decision

StarterTemplate is the sole template repository for future application clones. A clone must support independent account administration and adding a POS feature without editing IAM internals. Its code, setup documentation and tests must be self-contained; no separate repository or extraction SHA is part of the downstream workflow.

Keep .NET 10, the modular monolith, Clean Architecture boundaries, bounded-context feature folders, and API-mediated Blazor flows. Do not add a Person/Party framework, new mediator, module loader, or package split during this refactor.

Work exclusively in StarterTemplate. Do not merge or deploy automatically. Keep credentials in user-secrets or deployed secret stores, and give every clone its own repository identity, host secret IDs, ports, infrastructure resources and configuration.

Repository owner/visibility and namespace prefix must be settled before remote creation and renaming. Default recommendation: same owner, private initially; choose a dedicated technical prefix during setup. Use the existing rename script and verify it rather than introducing another packaging system now.

## Retention inventory

| Area | Retain | Boundary |
| --- | --- | --- |
| IAM | Accounts, credentials, roles, permissions, direct grants, activation/invitation, recovery, disabling, sessions, MFA, passkeys, SSO, private avatars | No Customer/Employee/Department or KYC dependency |
| Persistence | Generic repository, UoW/transactions, specifications, audit, soft delete, concurrency, provider support | IAM, Shared, ControlPlane retained contexts only |
| Platform | Logging, tracing, metrics, health, validation, sanitized responses, resilience, tenant-aware caching, feature flags | No business vocabulary required to operate |
| Messaging | Domain/integration events, outbox, retry, failed messages, background execution, notifications | Remove only Banking/HR producers, consumers, templates and registrations |
| UI | Authenticated shell, native authorization, admin users/roles/permissions/menus, shared components, API clients | No business links, default business menus or Department selectors |
| Tenancy | Tenant resolution/isolation, stamp routing, controlled provisioning and audit | Neutral approval; no mandatory business KYC |
| Delivery | Rename tooling, CI, deployment/IaC, local Compose, provider configuration, Data Protection | New repository identity; remove original deployment bindings |

Payments, reporting, mobile and advanced provisioning remain during the first refactor. Decide optional-module packaging separately. Their presence is not evidence of runtime certification.

## Removal and refactor inventory

| File group or integration point | Action |
| --- | --- |
| Features/Banking across Domain, Application, Persistence, Infrastructure, API, SharedKernel, Validation, RCL and Web | Remove Customers, Directors, corporate details, classifications, contracts, CRUD, events, email composers and seeds |
| Features/HR across those layers | Remove Employees, Departments, manager relationships, numbering/sequences, contracts, CRUD, events and seeds |
| Web Components/Pages/Admin/AdminCustomers.razor, AdminEmployees.razor and department page | Remove routes, dialogs, models, services and imports |
| Application Features/Shared/Validation | Remove Create/Update/Delete Customer, Employee and Department validators that live outside their owning folders |
| IAM Users/Entities/AppUser.cs | Remove EmployeeId/CustomerId, linked factories/behaviors and business events; review NationalId, RegistrationNumber, DateOfBirth and HR Gender dependency; keep minimal auth/display/contact data |
| IAM Users/Entities/AppRole.cs and Menus/Entities/MenuItem.cs | Remove DepartmentId and its scope in DTOs, handlers, persistence indexes and UI |
| IAM CreateAppUser and linked grant/revoke/link handlers | Preserve independent account operations; remove Banking/HR UoW dependencies and business lookup requirements |
| Auth responses/mappings, login/refresh/TOTP/OTP/passkey/SSO and Web AuthSession | Remove linked business IDs consistently; retain claims, tokens, sessions and tenant identity |
| IAM user searches/forms/views and profile contracts | Remove business filters, labels, lookups and KYC inputs |
| IAM role enum, PermissionSeed, MenuItemSeed, IamReferenceDataSeed | Remove Employee/Customer defaults and Banking/HR resources/routes; retain extensible role/permission engine and platform permissions |
| DevelopmentIdentitySeeder | Replace named Employee-linked fixtures with configuration-driven bootstrap administrator and neutral test data |
| TenantModulesDialog and module catalog | Remove hard-coded HR/Banking choices; preserve generic module authorization/invalidation |
| ControlPlane TenantStatus, Tenant.ApproveKYC, command/handler/controller/UI/contracts | Introduce neutral approval terminology; preserve authorization and controlled transitions |
| Program.cs, module DI, infrastructure messaging registration, persistence helpers | Remove registrations and imports; verify each import is functional before treating it as a runtime dependency |
| Banking/Hr contexts, factories, UoWs, repositories, migrations | Remove from StarterTemplate only; retain generic persistence tools |
| deploy-azure.yml and non-azure-deploy.yml | Remove HR/Banking bundle generation/execution and connection variables; audit all scripts, IaC and host configuration for the same dependencies |
| Architecture tests, integration fixtures and isolation tests | Remove business fixtures; retain and generalize boundary, caching, migration and tenant-isolation protection |
| AGENTS.md, PLAN.md, strategy, README and architecture/development docs | Replace business reference instructions in StarterTemplate; preserve generic compact-list UX guidance |

Resolve remaining references by code inspection, not global text deletion. Payment CustomerReference is a provider correlation field and must not be deleted merely because its name includes Customer.

## Execution phases and gates

### Phase 0 Independent template foundation

- [x] Establish the private StarterTemplate repository and ST namespace.
- [x] Keep local secret stores, ports, volumes and deployment identities independent.
- [x] Verify API/Web builds, unit/architecture tests and a disposable namespace rename.
- [x] Provide self-contained clone setup guidance; remove extraction SHA from downstream setup.

Gate: the template stands on its own, contains no local secrets and has independently configurable runtime/deployment identities.

### Phase 1 Independent IAM

- Refactor account creation, profile and administration before deleting business modules.
- Remove business links from every response, request, claim/state mapping and event.
- Preserve activation email, recovery, access revocation, refresh/session termination and audit actors.
- Remove Department scope from roles/menus while retaining tenant/permission/feature/module constraints.
- Rework neutral bootstrap identity and catalog seeds.

Gate: account lifecycle runs with no Banking/HR service dependency. Unauthorized and forbidden paths remain protected. Auth-state and token/session contracts are consistent across all login methods.

### Phase 2 Remove Banking and HR

Delete the inventory above in coherent slices; remove registrations, validators outside feature folders, navigation, templates, business test fixtures and settings together. Retain platform tests that formerly used business entities by replacing fixtures with platform entities or test-only neutral fixtures.

Gate: API and Web build; dependency searches show no active Banking/HR domain dependency; platform authorization, caching and tenant-isolation coverage retained.

### Phase 3 Neutral tenant approval

Change business KYC to administrative approval, with consistent API/UI/event terminology. Keep approval permission, audit actor, controlled provisioning, suspension and active-only routing. Product-specific compliance checks are downstream policy.

Gate: transition, unauthorized, forbidden and tenant routing tests pass; no path provisions or routes an unapproved tenant accidentally.

### Phase 4 Fresh database and delivery baseline

StarterTemplate initially targets fresh databases. Upgrades of existing application databases are outside this refactor. Once the retained model settles, generate clean initial migrations separately for each retained provider-specific context into Features/{Context}/Migrations/{SqlServer|PostgreSql}. Verify design-time factories use provider-compatible fallback configuration.

Align history tables, bundles, workflow execution, connection names, fixtures, health checks and optional provider settings. Rewire repository/environment/OIDC identifiers for the new repository; do not reuse original production targets.

Gate: both providers migrate an empty database; no pending model changes; migration scripts/bundles generate; deployment configuration has no removed-context or original-target dependency. Do not apply to existing databases.

### Phase 5 Starter certification

Run API/Web builds, unit, architecture and SQL Server/PostgreSQL integration tests. Browser/email smoke: bootstrap login, account invite/activation, password recovery, MFA/passkey/SSO where configured, permission changes, forbidden direct URL, logout, disabled user and refresh/session revocation. Verify rename/start using a disposable clean checkout. Add a test-only neutral vertical slice to prove API-to-application-to-persistence flow without shipping a new demonstration business domain.

Gate: a downstream developer can rename, configure, migrate, run, sign in and introduce a POS slice without editing IAM. Document any optional capabilities that were not exercised.

## Existing readiness items

The Fido2 4 migration fixes compilation and options generation; it does not certify real authenticator ceremonies. Current credential uniqueness and user-handle ownership callbacks return true. Before passkeys are declared ready in StarterTemplate, replace them with validated credential ownership/uniqueness checks and cover failure cases. Track malformed input, challenge consumption/replay, user verification and tenant scope in that focused follow-up.

Retain a readiness item for deployment-specific forwarded-header proxy trust. Preserve the existing targeted-review findings note and resolve its operational items separately. Dependency/analyzer warnings and mobile runtime checks must be reported honestly.

## Check commands

- dotnet build src/Backend/Api/ST.Api/ST.Api.csproj --no-restore -p:UseSharedCompilation=false
- dotnet build src/Frontend/Web/ST.UI.Blazor/ST.UI.Blazor.csproj --no-restore -p:UseSharedCompilation=false
- dotnet test tests/ST.Tests.Unit/ST.Tests.Unit.csproj --no-restore -p:UseSharedCompilation=false
- dotnet test tests/ST.Tests.Architecture/ST.Tests.Architecture.csproj --no-restore -p:UseSharedCompilation=false
- dotnet test tests/ST.Tests.Integration/ST.Tests.Integration.csproj --no-restore -p:UseSharedCompilation=false
- git diff --check

Run focused checks per slice and full provider/browser certification at the final gate. Each phase ends in a focused commit/PR with evidence and remaining limits. Do not call the new starter ready based on compilation alone.

Phase 0 verified: independent private repository; host secret IDs, JSON/script syntax, API/Web builds, architecture/unit tests and a disposable namespace rename passed. Azure deployment remains disabled. Setup depends only on StarterTemplate.

### Phase 1 checkpoint: independent bootstrap administrator

- [x] Replace named Employee-linked bootstrap fixtures with one configured system administrator.
- [x] Require explicit credentials and nonempty tenant identity when bootstrap is enabled.
- [x] Preserve existing account lifecycle and passwords on ordinary reruns; password reset is explicit and Identity failures are checked.
- [x] Reject conflicting tenant/name/email identities, business-linked, disabled and deleted accounts before administrative role writes.
- [x] Remove bootstrap deletion of unrelated accounts and seed only the platform administrator role.
- [x] Verify 12 focused cases; full unit suite 49/49 and architecture suite 80/80; API and Web builds passed.

Remaining Phase 1 at the bootstrap checkpoint included response/authentication decoupling, now completed by the neutral-account checkpoint below. The current remaining work is listed in the latest checkpoint. Phase 1 is not closed. Banking/HR fixtures and migrations are retained until their removal phases. SQL Server/PostgreSQL integration and browser/email smoke are pending.

### Phase 1 checkpoint: standalone account administration

- [x] Create/edit request contracts and form no longer accept Employee/Customer links, ID numbers or HR Gender.
- [x] Creation uses the resolved tenant and calling actor; the handler needs only IAM services.
- [x] Account, membership and contact profile writes share one IAM transaction; expected Identity failures trigger rollback before returning sanitized responses.
- [x] Profile editing refuses another tenant's account and preserves lifecycle and audit creator.
- [x] Validate write requests through shared FluentValidation validators and batch-check selected roles.
- [x] Account-created domain/integration events carry account/tenant/contact data only.
- [x] Profile form edits retain all existing memberships; new accounts have no implicit default role.

Verification uses real SQLite transactions for role, profile and event-publication rollback and tenant/identity conflicts. SQL Server/PostgreSQL integration, browser/email flows and fresh migration certification remain separate gates. No entity columns or physical schema were removed in this slice. Existing response fields and legacy account activation paths remain on the checklist.

Verification: API and Blazor builds passed; 62 unit tests (including 13 standalone-account cases) and 80 architecture tests passed. A disposable copy renamed its solution and namespace ST -> CS -> DX, updated prefix metadata and regenerated both host secret identities without ORIGIN.md. Provider/browser certification remains pending.

### Phase 1 checkpoint: neutral account responses and authentication state

- [x] Remove Employee/Customer/Staff identifiers, national ID and HR Gender from account/current-user/admin-list responses.
- [x] Use one backend account mapper for password, OTP, TOTP, passkey, SSO, refresh and administration; correct the previous positional field mismatch.
- [x] Preserve actual email confirmation, MFA enrollment, tenant, session, permissions, roles and private profile-image routing in login/current-user state.
- [x] Remove employee/customer claims and business identity context; retain platform account and tenant identifiers.
- [x] Remove linked-record labels and category filters from account administration.

Intentional contract change: API and clients use the same StarterTemplate baseline revision. No persisted columns or migrations changed. Existing token/session lifecycle and authentication checks are retained. Full browser/email/passkey/SSO certification remains pending.

Remaining Phase 1: legacy AppUser fields/factories, employee/customer ID search parameters used by temporary employee administration, access-event contracts and grant/revoke/link handlers, independent activation/invitation, Department scope and neutral catalogs. Banking/HR module removal and fresh provider migrations follow in later phases.

Verification for neutral authentication: API and Blazor builds passed; full unit suite 74/74, architecture suite 80/80 and whitespace checks passed. Twelve new mapping/claims/current-user cases cover distinct identity fields, neutral wire shape, platform permissions/session/tenant/MFA state and private image routes. Existing analyzer/package warnings remain. Browser authentication ceremonies and fresh migration certification are pending.
