# StarterTemplate domain neutral refactor plan

Date: 2026-10-06
Status: Phases 0–5 complete for the core starter. Automated certification passed; Visual Studio hosts run, live bootstrap API login passed, and the user confirmed browser sign-in and viewing items. Optional external-provider certification is recorded separately.

## Objective and repository decision

StarterTemplate is the sole template repository for future application clones. A clone must support independent account administration and adding a POS feature without editing IAM internals. Its code, setup documentation and tests must be self-contained; no separate repository or extraction SHA is part of the downstream workflow.

Keep .NET 10, the modular monolith, Clean Architecture boundaries, bounded-context feature folders, and API-mediated Blazor flows. Do not add a Person/Party framework, new mediator, module loader, or package split during this refactor.

Work exclusively in StarterTemplate. Do not merge or deploy automatically. Keep credentials in user-secrets or deployed secret stores, and give every clone its own repository identity, host secret IDs, ports, infrastructure resources and configuration.

Approved identity: private `odhiamboally/starter-template`, namespace `ST`, directory `E:\Repos\StarterTemplate`. Use the existing rename script for downstream clones.

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

Run API/Web builds, unit, architecture and SQL Server/PostgreSQL integration tests. Certification combines user-confirmed browser sign-in/navigation with automated API and real-email checks for account invite/activation, password recovery, configured MFA, forbidden access, logout, disabled users and refresh/session revocation. Physical passkeys, external SSO and external payment providers require separate configured integration checks; their absence does not reopen core refactor certification. Verify rename/start using a disposable clean checkout. Add a test-only neutral vertical slice to prove API-to-application-to-persistence flow without shipping a new demonstration business domain.

Gate: a downstream developer can rename, configure, migrate, run, sign in and introduce a POS slice without editing IAM. Document any optional capabilities that were not exercised.

## Existing readiness items

Passkey registration/assertion now require user verification and validate active verified account state, global credential uniqueness, tenant ownership and user handles. Persisted tenant-scoped challenges expire and are consumed once atomically; tests cover malformed requests, foreign tenants and concurrent replay. Physical authenticator ceremonies remain unverified. External SSO and mobile runtime are optional capabilities not exercised in this certification.

Stripe and M-Pesa sandbox/live payment execution and callbacks remain unverified. Their configuration fields are initialized in API user-secrets without overwriting existing values; blank credentials deliberately leave providers unconfigured.

Retain a readiness item for deployment-specific forwarded-header proxy trust. Preserve the existing targeted-review findings note and resolve its operational items separately. Dependency/analyzer warnings and mobile runtime checks must be reported honestly.

## Check commands

- dotnet build src/Backend/Api/ST.Api/ST.Api.csproj --no-restore -p:UseSharedCompilation=false
- dotnet build src/Frontend/Web/ST.UI.Blazor/ST.UI.Blazor.csproj --no-restore -p:UseSharedCompilation=false
- dotnet test tests/ST.Tests.Unit/ST.Tests.Unit.csproj --no-restore -p:UseSharedCompilation=false
- dotnet test tests/ST.Tests.Architecture/ST.Tests.Architecture.csproj --no-restore -p:UseSharedCompilation=false
- dotnet test tests/ST.Tests.Integration/ST.Tests.Integration.csproj --no-restore -p:UseSharedCompilation=false
- git diff --check

Run focused checks per slice and full provider/browser certification at the final gate. Each phase ends in a focused commit/PR with evidence and remaining limits. Do not call the new starter ready based on compilation alone.

## Current implementation and certification evidence

- Independent bootstrap and IAM account creation/editing, activation, disabling, invitation and recovery have no business-record dependency. Passwords are never included in invitation emails. Identity failures and event failures roll back supported account writes.
- Banking and HR source, contracts, services, UI, catalogs, registrations and historical migrations are removed. Department scope is removed from IAM. The retained contexts are IAM, Shared and ControlPlane.
- Tenant approval is administrative, permission protected and audited. Suspended or unapproved tenants cannot route through cached connection metadata.
- Six provider-specific InitialStarter migrations target **fresh databases only**. Both providers migrate empty databases, rerun migrations and execute generated idempotent scripts. Shared includes Quartz tables. PostgreSQL uses persisted application concurrency tokens; SQL Server retains native rowversion.
- All six provider migration scripts and Linux migration bundles generated successfully. Local isolated SQL Server was migrated for Visual Studio host verification. Azure deployment remains disabled.
- A test-only neutral vertical slice verifies permission protection, tenant isolation and API/Application/Persistence flow without adding a production business module.
- Automated HTTP and real Mailpit SMTP verify invitation acceptance/replay, bearer sign-in, forbidden access, logout, OTP password recovery, disabled accounts and session/refresh revocation. Configured TOTP and persisted provider concurrency are included in final checks.
- Final full suites: 131 unit tests, 73 architecture tests and 23 integration tests passed; the normal integration run skipped one opt-in RabbitMQ test, which passed separately against the local broker. API and Blazor builds passed. Configured TOTP enrollment/login/disabled-account rejection and persisted concurrency passed for both providers. A clean committed checkout renamed ST -> CS -> DX, regenerated independent host secret identities, restored and built both hosts, and passed the same full unit, architecture and provider/HTTP/email suites. GitHub backend, frontend and architecture CI passed for implementation commit 350e11d. The hosts now listen on their configured ports. Live API bootstrap sign-in passed and correctly requires administrator MFA enrollment. The stale hardcoded administrator email hint was removed; credentials come from API user-secrets. The user confirmed successful browser sign-in and viewing items. This browser acceptance, together with the automated changed-flow tests, completes core Phase 5.

No merge or deployment is authorized by this plan. Existing package/analyzer warnings, deployment-specific proxy trust and the targeted review operational findings remain documented limitations.
