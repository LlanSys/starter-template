# StarterTemplate - Execution Strategy

> Current work: [domain-neutral refactor plan](docs/architecture/domain-neutral-refactor-plan.md). Clone setup and verification limits: [starter-setup.md](docs/development/starter-setup.md). StarterTemplate is self-contained; no separate repository or extraction revision is required.

> Last updated: 2026-07-03
>
> This file explains how to execute `PLAN.md` without jumping between unrelated work.
> `PLAN.md` defines what the template should become. This file defines sequencing, dependencies, readiness gates, and safe parallel work.

---

## 1. Purpose

The StarterTemplate roadmap is intentionally broad. Many items depend on other items being complete enough before they can integrate well.

This strategy prevents three common problems:

- Starting advanced features before the foundation they depend on exists.
- Reworking earlier code because dependent phases were implemented in the wrong order.
- Spending time on maturity polish before core production flows are working.

The goal is steady progress through working vertical slices.

---

## 2. Execution Principles

- Prefer completing one working vertical slice over touching many incomplete areas.
- Do not start a dependent feature until its prerequisite is implemented to the readiness gate defined here.
- Core correctness, security, and data integrity are not maturity tasks.
- Maturity tasks are allowed only when the current implementation is production-safe but not maximally configurable.
- Every meaningful implementation should update `PLAN.md` or this file when the roadmap or dependency understanding changes.
- Every meaningful implementation should end with a concise commit message.
- Local build/test/architecture checks should run whenever practical before calling work complete.
- Persistence work should follow `docs/architecture/persistence-standards.md`.
- SaaS tenancy and deployment-stamp decisions must be settled before cloud configuration, Key Vault, or deployment workflows are called production-ready.

---

## 3. Critical Path

This is the default execution path unless a blocker forces a dependency detour.

1. SaaS tenancy and deployment-stamp model
2. Local running solution
3. IAM/authentication completeness
4. Customer CRUD reference flow
5. Exception handling and validation consistency
6. Pipeline behaviours and transaction conventions
7. RBAC/ABAC/permission-based authorization
8. Feature flags
9. Dynamic menu/module loading
10. Health checks, audit, observability, and security hardening
11. Docker/local developer platform
12. Azure deployment, Key Vault, monitoring, and APIM
13. Entra ID SSO
14. Rich reusable template capabilities: SignalR, reporting, payment abstractions, mobile

Current near-term focus:

- Keep the local API and Blazor UI runnable from Visual Studio.
- Treat SaaS stamp configuration as the source of truth for Key Vault, resource groups, ACA/App Service settings, database, cache, storage, and messaging.
- Certify the IAM/Auth baseline with local browser/email smoke testing.
- Close platform storage hardening: profile media provider configuration and Data Protection key persistence.
- Reconcile API security, exception propagation, validation coverage, MassTransit/outbox, and health checks before moving into deployment work.
- Keep the Customer reference flow as the pattern for future feature slices.

---

## 4. Dependency Map

| Work Item | Depends On | Why |
| --- | --- | --- |
| Cloud configuration and Key Vault alignment | SaaS tenancy and deployment-stamp model | Secret names, managed identities, resource groups, and provider settings depend on whether a stamp is pooled or isolated. |
| IaC modules | SaaS tenancy and deployment-stamp model | Bicep/Terraform modules need stable stamp boundaries and optional add-on rules. |
| Blazor authenticated shell | IAM login, token/session handling | The shell needs reliable current-user state. |
| Customer CRUD reference flow | API auth, customer endpoints, DTO contracts | It proves UI to API to Application to Persistence and back. |
| Admin users/roles/permissions UI | IAM API completeness, authorization model | Admin screens must manage real identity data, not placeholders. |
| RBAC/ABAC/permission model | IAM users, roles, claims, policies | Permissions need stable user/role/claim primitives. |
| Direct user permission grants | Permission model | Exceptions beyond role membership need a permission catalog first. |
| Dynamic menus from DB | RBAC/permission model, feature flags | Menus must be filtered by real authorization and feature availability. |
| Feature-gated UI | Feature flag abstraction | UI visibility must use the same feature gate model as API/Application. |
| Endpoint authorization policies | IAM claims and permission model | Policies need known claims/permissions to evaluate. |
| Audit trail | Current user/context resolution | Audit records must know who performed an action. |
| Current user audit stamping | IAM current-user resolution, explicit system actor convention | Audit fields must capture the real actor without breaking seeders, background jobs, or integration consumers. |
| Domain encapsulation hardening | Stable EF mappings, aggregate boundaries, seed/migration confidence | Setter restrictions should be applied per aggregate to avoid broad EF/Identity breakage. |
| IAM auth integration tests | IAM endpoints, token/session persistence, MFA policy, permission policies | Tests should verify security behavior after the flow is implemented, not before the contracts settle. |
| Soft delete consistency | EF configurations, repository/UoW conventions | Query filters and delete behavior must be enforced centrally. |
| Health checks | Dependency configuration | Checks need known SQL, Redis, messaging, Seq, Key Vault, and provider settings. |
| Observability | Logging conventions, health/dependency map | Traces/metrics/logs need stable operation boundaries. |
| Docker Compose | Local dependency settings | Compose should match actual appsettings and ports. |
| Azure deployment | Local run, CI, config/secrets, health checks | Deployment should not be automated before the app can prove readiness. |
| Key Vault | Configuration model | Secrets need established settings POCOs and section names. |
| Entra ID SSO | IAM AppUser linking model | External identities must map into local users/employees safely. |
| Profile media | Storage abstraction, IAM/HR/Banking ownership rules | Account avatars and business-domain photos/documents must not become one mixed upload bucket. |
| Kenya verification providers | Customer/Employee identity model, resilience, audit | Verification APIs need stable identity fields, provider adapters, audit, and secure PII handling. |
| SignalR | Auth and user/group strategy | Hubs need secure user, role, tenant, and context grouping. |
| QuestPDF reporting | Feature data and storage/download strategy | Reports should be generated from real features and delivered consistently. |
| Payments | Domain feature needing payments, provider abstraction | Provider integrations need idempotency, audit, callbacks, and reconciliation. |
| Product modules such as loans | StarterTemplate completed and cloned downstream | SACCO/product-specific behavior belongs in the downstream solution, not the reusable template core. |

---

## 5. Readiness Gates

### Gate 0 - SaaS Tenancy And Stamp Model Ready

Required before calling cloud configuration, Key Vault alignment, or deployment workflows production-ready:

- Tenant, deployment stamp, pooled stamp, and isolated stamp are defined.
- Configuration ownership is clear for user-secrets, environment variables, host settings, and Key Vault.
- Resource group, Key Vault, storage, cache, database, messaging, and observability boundaries are documented.
- Control-plane tenant catalog fields are identified.
- IaC module boundaries and optional add-on rules are defined.
- Tenant isolation tests are planned or implemented.

### Gate 1 - Local Running Solution

Required before heavy feature expansion:

- API runs locally from Visual Studio.
- Blazor UI runs locally.
- SQL Server connection is configured.
- Development seed/admin login works.
- Scalar/OpenAPI is reachable.
- Relevant local build commands pass.

### Gate 2 - IAM/Auth Ready

Required before protected admin modules, dynamic menus, and authorization-heavy UI:

- Login works through API.
- Refresh token flow works.
- Logout clears local session.
- Current-user endpoint returns stable user identity, roles, claims, and context links.
- TOTP setup and verification work.
- Employee and Customer AppUser links are demonstrable.
- Grant System Access sends the activation email and refuses duplicate active links.
- Revoke System Access deactivates the linked account and terminates active sessions/refresh tokens.
- Account lockout, password policy, session expiry, refresh-token reuse detection, and MFA-required behavior are verified.
- Critical auth paths have automated tests or documented manual verification.
- IAM-affecting writes stamp the real current user or an explicit system actor.

Current status:

- Code-level IAM/Auth baseline is complete.
- Application-level IAM validation is wired.
- Runtime audit stamping is actor-aware.
- Unit and architecture guardrails cover the implemented validation, audit, MFA, permission, and API authorization conventions.
- Final gate item is local browser/email smoke testing.

### Gate 3 - Customer Reference Flow Ready

Required before cloning patterns into Employees, Users, Roles, Permissions, and future business modules:

- Customer list loads real DB records.
- Search, filters, pagination, view, create, edit, and delete work.
- Delete uses confirmation.
- User-facing errors are meaningful.
- API failures are logged through source-generated LoggerMessage.
- Shared API client and endpoint configuration are used.
- UI table/action/dialog conventions are reusable.

### Gate 4 - Authorization Ready

Required before dynamic menus and permission-managed admin surfaces:

- Permission catalog exists.
- Roles can be assigned permissions.
- Users can receive direct permission grants where needed.
- Policies or permission attributes protect APIs.
- Application-level checks exist where endpoint checks are not enough.
- UI can query allowed capabilities for the signed-in user.
- Tests cover role permission, direct grant, forbidden, and unauthorized cases.

### Gate 5 - Feature Flags Ready

Required before feature-gated modules and rollout flows:

- Application-owned feature gate abstraction exists.
- Local configuration supports flags.
- Azure App Configuration compatibility is preserved.
- API/Application/UI can evaluate flags consistently.
- Disabled features fail closed where appropriate.

### Gate 6 - Dynamic Menus Ready

Required before removing static admin/menu definitions:

- Menu/module catalog is modeled and persisted.
- Nested menu items are supported.
- Menus are filtered by permission, role, feature flag, tenant/context where applicable.
- Direct URL access remains protected by server-side policies.
- Menu cache invalidation is defined.

### Gate 7 - Deployment Ready

Required before Azure deployment automation is considered complete:

- CI builds and tests relevant projects.
- Health checks include real dependencies.
- Secrets are externalized.
- Release workflow builds deployable artifacts.
- Deployment has smoke tests and rollback guidance.
- Logs/traces/metrics are visible in the selected monitoring stack.

---

## 6. Safe Parallel Work

These can be worked on in parallel when they do not block or destabilize the critical path:

- Documentation updates.
- CI workflow naming and small workflow improvements.
- UI theme polish that does not rewrite routing/auth/data flow.
- Architecture tests for conventions already agreed.
- Local run documentation.
- Docker Compose after local dependency settings are stable.
- OpenAPI/Scalar improvements.
- Reporting abstractions before full business reports.
- Background job examples after configuration is stable.

Parallel work must still integrate through normal build/test checks.

---

## 7. Work To Avoid Too Early

Avoid these until their gates are ready:

- DB-backed dynamic menus before permissions and feature flags exist.
- Admin permission UI before permission model behavior is clear.
- External verification provider integrations before Customer/Employee identity evidence and audit models are stable.
- Azure deployment before local run, health checks, and secret strategy are stable.
- Entra ID SSO before AppUser linking and claims model are stable.
- SACCO/product modules before StarterTemplate platform foundations are reliable and cloned downstream.
- Plugin-driven modules before static/dynamic module catalog is proven.
- Heavy maturity polish before a working local product demo exists.

---

## 8. Session Workflow

At the start of a session:

1. Read `PLAN.md`.
2. Read this file.
3. Check `git status`.
4. Identify the current critical-path gate.
5. Choose the next task that satisfies or unblocks that gate.

During a session:

1. Prefer one bounded task.
2. Avoid changing unrelated layers unless the dependency requires it.
3. Update the user when a dependency or sequencing assumption changes.
4. Keep convention changes documented.

At the end of a session:

1. Run relevant checks where practical.
2. Update `PLAN.md` or this file if scope changed.
3. Generate a concise commit message.
4. Note any remaining blocker clearly.

---

## 9. Current Next Moves

Recommended immediate order:

1. Certify Phase 0: SaaS tenancy, deployment-stamp model, and configuration ownership.
2. Certify Phase 1: IAM/Auth and local UI/API smoke testing.
3. Certify Phase 2: platform storage, cache, exception, validation, and messaging hardening.
4. Certify Phase 3: health checks, observability, API security/deprecation/throttling, and operational diagnostics.
5. Certify Phase 4: CI/CD deployment readiness, migration bundles, Docker local platform, and Azure App Service release flow. (Complete)
6. Move product-specific work such as SACCO loans, KYC/CRB, AML, payroll, or full HR into downstream solutions cloned from StarterTemplate.

---

## 10. Phase Register

Use this register as the working execution checklist. A phase is not considered closed until its gate is tested, documented, and committed.

| Phase | Status | Scope | Gate |
| --- | --- | --- | --- |
| Phase 1 - IAM/Auth Enterprise Baseline | Implemented, needs final smoke certification | Login, refresh, logout, sessions, lockout, TOTP MFA setup/disable, admin MFA enforcement, grant/revoke system access, current user, profile picture upload, inactivity warning | Browser smoke: login, MFA challenge/setup/disable, grant email, revoke access, refresh, logout, session timeout |
| Phase 2 - Platform Storage, Cache, Exceptions, Validation, Messaging | In progress | Azure Blob profile storage, Data Protection Blob/Key Vault keys, HybridCache/output cache, ProblemDetails propagation, FluentValidation coverage, MassTransit outbox | Build/test plus targeted smoke for profile upload, error messages, lookup cache, outbox publish |
| Phase 3 - API Security And Operational Readiness | In progress | security headers, CORS, configurable rate limiting policies, response compression, API version/deprecation headers, deep health checks, PII log masking, correlated tracing, dependency vulnerability hygiene | Health endpoints prove SQL/Redis/storage/Key Vault where enabled; logs are safe; API communicates deprecation/throttling clearly; package advisories are resolved or explicitly risk-accepted |
| Phase 4 - Deployment And Release Engineering | Implemented | OIDC-authenticated efbundle migration pipeline, Docker Compose, GitHub Actions release, Azure App Service deployment slots, Key Vault wiring, rollback notes | CI builds/test/releases, migration bundle generated, Azure deploy smoke passes |
| Phase 5 - Template Extensibility | In progress | feature flags, permission-aware dynamic menus, SignalR baseline, reporting abstraction, payment gateway abstractions, Entra SSO, passkeys, RCL consolidation | Feature gates and dynamic menus fail closed; reusable examples exist without domain-specific SACCO logic; passkeys have real flows before being marked complete |

---

## 11. Phase Closure Checklist

Before we move from one phase to another:

- Confirm the phase scope has no known runtime dummy/fake implementation.
- Confirm settings POCOs have matching JSON sections.
- Confirm user-facing errors are meaningful and backend exception details are not leaked.
- Confirm source-generated logging exists for catches and important failure paths.
- Run relevant build, unit, architecture, and integration tests.
- Run local browser smoke where the phase affects UI/auth.
- Update `PLAN.md` and this strategy file.
- Create a concise commit message and PR.
- Tell the user explicitly that the phase is ready to move forward.

## StarterTemplate direction approved 2026-10-06

StarterTemplate is the independent template for downstream applications. Execute the domain-neutral refactor plan in this repository. Temporary Banking/HR examples are pending removal and do not define downstream requirements. Keep clone guidance self-contained and validate each retained platform slice.

## StarterTemplate checkpoint 2026-10-06

Phase 0 is complete: independent private repository, ST identity, independent host secret IDs/ports, deployment pause and disposable rename verification. Phase 1 has started with an independent configured bootstrap administrator; 49 unit tests and 80 architecture tests pass, and API/Web build. Remaining account/domain decoupling is tracked in docs/architecture/domain-neutral-refactor-plan.md. This checkpoint does not certify providers, browser flows, mobile or fresh database migrations.

## Independent account administration checkpoint

Clone setup is self-contained in docs/development/starter-setup.md; no source repository or extraction SHA is required. Create/edit inputs, handlers and account-created events are now business-neutral, with resolved tenant/audit identity and one IAM transaction. Account response/auth mappings, independent activation and remaining business-model removal still require follow-through in the refactor plan.

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

### Phase 1 checkpoint: independent account access lifecycle

- [x] Add activation by IAM account ID through the API, typed client and account actions; use users.edit permission for activation and users.deactivate for deactivation.
- [x] Require a resolved matching tenant and a non-deleted account for both operations.
- [x] Preserve password, email verification and memberships when activating; set the existing password-change flag and rotate the security stamp. Repeated activation preserves active sessions and lifecycle audit.
- [x] Atomically deactivate the account, rotate its security stamp, terminate sessions and revoke refresh tokens. Clean stale access even when the account is already inactive without replacing its original deactivation actor.
- [x] Use account/tenant/actor access-event contracts without Employee/Customer links and dispatch them before transaction commit.
- [x] Validate the signed JWT session claim ahead of client headers; revoked API sessions return 401 without cookie sign-out.

Verification: API and Blazor builds passed; unit suite 91/91, architecture suite 80/80, JSON parsing and whitespace checks passed. Seventeen new tests cover lifecycle/credential preservation, tenant/deleted-account rejection, Identity/event failure rollback, session/token cleanup, endpoint permission requirements and revoked/valid bearer sessions. Existing analyzer/package warnings remain.

Remaining Phase 1: invitation/email-confirmation delivery and browser certification, legacy AppUser fields/factories, employee/customer ID search bridge and employee grant/revoke/link handlers, Department scope and neutral catalogs. No invitation was sent, no passwords were emailed or reset by the new activation flow, and no schema or migration changed. Banking/HR removal and fresh provider migration certification remain later phases.
