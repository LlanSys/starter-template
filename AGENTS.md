# StarterTemplate Agent Instructions

> Current work: [domain-neutral refactor plan](docs/architecture/domain-neutral-refactor-plan.md). Clone setup and verification limits: [starter-setup.md](docs/development/starter-setup.md). StarterTemplate is self-contained; no separate repository or extraction revision is required.

Work only in this repository and its configured remote. Downstream setup and ongoing implementation do not depend on another repository or an extraction SHA.

This file is the canonical source of truth and working contract for AI coding tools in this repository. If another tool-specific file conflicts with this one, this file wins and the other file should be updated.

## Start Every Coding Or Debugging Task

1. Read this `AGENTS.md` first.
2. Read `PLAN.md` and `PLAN_EXECUTION_STRATEGY.md` before phase or architecture work.
3. Read the closest relevant docs before changing code:
   - `docs/architecture/feature-folder-convention.md`
   - `docs/architecture/persistence-standards.md`
   - `docs/architecture/saas-multitenancy-strategy.md`
   - `docs/architecture/ui-to-backend-flow.md`
   - `docs/development/environment-configuration-checklist.md`
   - `docs/development/local-platform.md`
   - `docs/development/azure-storage-configuration.md`
   - `docs/development/non-azure-deployment.md`
   - `docs/development/configuration-code-conventions.md`
4. Inspect the existing code before proposing or writing changes.
5. If a convention changes, update this file and the relevant docs in the same PR.

## Project Identity

- Repo: `LlanSys/starter-template`
- Product: StarterTemplate
- Root namespace prefix: `ST`
- Rename a clone with `scripts/rename-template.ps1 -NewName <Name> -NamespacePrefix <XX> -DatabaseProvider <SqlServer|PostgreSql|Both> -ResetGitHistory` before anything else, including its first run, commit or deployment. The script keeps only the chosen database provider and replaces the template's git history with a fresh repository. See docs/development/renaming-the-template.md.
- After protected data has been issued, `DataProtection:ApplicationName` is immutable without an explicit migration or security reset.
- Target framework: `.NET 10`
- Architecture: modular monolith with Clean Architecture boundaries and bounded-context feature folders.
- Retained platform contexts: `IAM`, `Shared`, and `ControlPlane`. Application business contexts belong in downstream clones.

## Architecture Rules

- Domain and SharedKernel have no solution project dependencies. Persistence references Domain only and implements repository and Unit of Work contracts defined there.
- Application references Domain, SharedKernel, and the server-side SharedKernel.Validation project. Infrastructure references Application and Persistence for provider, Identity, and configuration integration. API composes Application, Infrastructure, and Persistence.
- Blazor references SharedKernel and reaches backend features through typed HTTP clients. Keep UI contracts in the owning Blazor feature.
- Keep feature-owned code inside `Features/{Context}/{Feature}` in every layer where that code exists. Keep host registration, middleware, app shell and session restoration, logging, generic caching and repositories, JSON helpers, and other application-wide infrastructure outside feature folders.
- A feature workflow uses the context's own persistence. Cross-context calls use an operation, identifier, projection, or event from the owning context. Keep the existing named data access exceptions and the architecture tests in sync with the [feature folder convention](docs/architecture/feature-folder-convention.md).
- IAM is independent of downstream product contexts. Product contexts use IAM through stable platform contracts and identifiers.

## Source Formatting

- Follow the BT-derived [readability conventions](docs/development/code-readability.md): four spaces, Allman braces, one statement per line, block-bodied procedural methods, logical blank lines and a soft 120-column target.
- Use adjacent partial classes for substantial Razor logic. Apply/check focused scopes with `scripts/Format-Code.ps1 -Include`. The build-only `ST.SourceFormatting` tool protects C# syntax and Razor literals; exclude migrations and generated files. Keep runtime packages free of formatter runtime dependencies.

## Backend Conventions

- Use MediatR request/handler flows already established in the repo.
- Use namespace using directives and short type names in handwritten C# and Razor. Use descriptive aliases for real collisions. Preserve generated migrations and string-based contracts.
- Inject `TimeProvider` for runtime clock reads and timers. Register `TimeProvider.System` at composition roots and replace it in tests. Capture one instant per decision and pass values into domain operations. Preserve timestamp fields, wire contracts, and deterministic seeds.
- Commands/queries and handlers should remain feature-owned.
- Each public top-level type must live in its own file named after that type. This applies to classes, records, structs, interfaces, and enums; do not bundle multiple public DTOs, validators, settings POCOs, entities, or helper types in one file.
- Use `AppResponse<T>` for expected business outcomes.
- Throw exceptions only for unexpected or exceptional failures.
- API responses must go through the established response/problem-details pattern.
- Do not leak exception type names, stack traces, provider errors, connection strings, or internal IDs into user-facing messages.
- All user-facing errors from the UI must be sanitized through the shared messaging pattern.
- Provider-specific integration DTOs belong beside the provider adapter. Shared contracts should stay provider-neutral and should not expose Stripe, M-Pesa, Azure, or other provider wire payloads.
- When a capability supports multiple runtime providers per operation, use a router/factory over isolated provider adapters. Do not make one provider adapter understand another provider's DTOs, credentials, or callbacks.
- Application-layer command/query handlers must use bounded-context Unit of Work interfaces (`ISharedUnitOfWork`, `IIamUnitOfWork`, etc.) for persistence orchestration, not raw `IRepository<T>`. Always call `CompleteAsync` with a `CancellationToken` after write operations.
- Pass `CancellationToken` to all async I/O calls (`ReadAsStringAsync`, `CompleteAsync`, `SaveChangesAsync`, etc.) where the parameter is available.
- Use `StringComparison.OrdinalIgnoreCase` for case-insensitive comparisons. Reserve `ToUpperInvariant()`/`ToLowerInvariant()` for value normalization (stored keys, wire formats, display strings) â€” never for equality checks. C# `switch` on normalized strings is acceptable when `StringComparison` is not supported by the expression.
- Always use `Guid.CreateVersion7()` instead of `Guid.NewGuid()` to ensure time-ordered identifiers.

## Persistence Rules

- Every declared `DbSet<T>` entity must have explicit EF configuration.
- Custom persistence context-related types use the project `DB` acronym, for example `IamDBContext`, `DBContextHelper`, and `ITenantFilteredDBContext`; keep Microsoft framework API names unchanged.
- Respect tenant isolation, soft delete, and audit actor conventions.
- Tenant-scoped writes must match the owning context. Never accept a tenant ID as write authority from a DTO. Include the persisted tenant key in detached write concurrency checks. Bulk operations must use an explicit tenant predicate in addition to the normal query filter.
- `CreatedBy`, `UpdatedBy`, `ActivatedBy`, `DeactivatedBy`, and `DeletedBy` must store stable actor identifiers, not display names or labels such as `DevelopmentSeed`.
- Prefer generic repository methods unless a concrete repository method is persistence-specific.
- Compose queries before materialization. Do not call `ToListAsync` before filters, tenant scope, paging, projection, or security scope are applied.
- Use `Any` for existence checks, not `Count > 0`.
- Use `CountAsync` only when the count is returned, logged, or used for a decision.
- Avoid N+1 query patterns. Batch identifiers and map results in memory.
- Cursor pagination must fetch `pageSize + 1` before trimming.
- **Database providers.** When adding EF Core migrations, generate one migration per provider present in the repository, using that provider's derived DbContext and an explicit output directory. Do not rely on default output directories. For the IAM bounded context:
<!-- <db:SqlServer> -->
  - `dotnet ef migrations add <Name> -c IamSqlServerDBContext -o Features/IAM/Migrations/SqlServer`
<!-- </db:SqlServer> -->
<!-- <db:PostgreSql> -->
  - `dotnet ef migrations add <Name> -c IamPostgreSqlDBContext -o Features/IAM/Migrations/PostgreSql`
<!-- </db:PostgreSql> -->
- **Provider markers.** Provider-specific code in shared files sits inside whole-line `<db:SqlServer>` / `<db:PostgreSql>` comment markers, written as independent per-provider blocks (no `if/else` across providers). Provider-only files follow the naming globs in `scripts/database-providers.json`; update that manifest when adding one. The template's marker guardrail test and `template-fork-check.yml` enforce this. See docs/architecture/database-provider-selection.md.
- **Database Context Factories:** When creating `IDesignTimeDbContextFactory<T>` implementations, always specify a provider-specific fallback connection string using `DesignTimeConfigurationFactory.GetConnectionString(..., fallbackConnectionName: "DefaultPostgreSqlConnection")` (or `"DefaultSqlConnection"`) to prevent parsing crashes from incompatible connection string parameters.
- **Idempotent Migrations:** EF Core index creations are globally intercepted by custom SQL generators (`IdempotentSqlServerMigrationsSqlGenerator` and `IdempotentNpgsqlMigrationsSqlGenerator`) to inject `IF NOT EXISTS` logic. Do not write manual raw SQL for idempotency on index creationâ€”the generator handles this automatically.

- The six InitialStarter migrations target fresh databases only. Never apply them as upgrades to an existing application database. Shared baselines include the provider-specific Quartz tables.
<!-- <db:PostgreSql> -->
- PostgreSQL uses application-stamped byte concurrency tokens.
<!-- </db:PostgreSql> -->
<!-- <db:SqlServer> -->
- SQL Server uses native rowversion.
<!-- </db:SqlServer> -->
- Passkey challenges and external sign-in authorization codes are temporary tenant-scoped records, deliberately hard-deleted on consumption or expiry. Credential ceremonies require user verification and persisted ownership/uniqueness checks.

## Validation, Logging, And Caching

- Add FluentValidation validators for write requests that can accept user input.
- Use source-generated `LoggerMessage` methods; do not add free-form logging in new code paths.
- Every `catch` block must log or deliberately translate the error into an expected result.
- Use `ICachableRequest` for cacheable queries and `ICacheInvalidatorRequest` for commands that invalidate cache.
- Keep cache keys tenant-aware where data is tenant-scoped. Cache factories must preserve request execution context and finish within the owning request scope, including cancellation cleanup. Tenant-wide mutation epochs invalidate optional user-scoped responses; read epochs from the shared cache across replicas. See docs/development/tenant-cache-isolation.md.

## IAM And Security

- IAM is a core platform capability, not an application-specific feature.
- Identity uniqueness is tenant-scoped, including deleted-account reservations. Use Identity through TenantUserStore for account admission/restoration; preserve transaction-owned per-tenant locks, MaxUsers human-seat accounting and explicit trusted proxy kinds. See docs/development/account-admission-and-kyc-safety.md.
- KYC originals remain quarantined until a real safety scan clears their exact checksum. Preserve durable scan leases/retries, definition freshness, clean-only download/acceptance and erasure-ledger gates. Rescanning never grants a manual clean override.
- Customer workspaces require KYC approval with a stable reviewer, a captured UTC instant, and an evidence reference before activation, provisioning retries, or first-administrator onboarding. Use `control_plane.approve_kyc` for approval. Preserve existing accounts during upgrades and obtain real review evidence for existing customer workspaces.
- KYC retention processing is enabled. Use versioned platform policies, explicit relationship-end events, indefinite legal holds with review dates, recoverable expiry, storage deletion receipts and a separate private erasure ledger. Never serve an erased object after restoring an older database; preserve that ledger independently.
- KYC evidence uses a private container and permission-protected API downloads. Client submission links are workspace-scoped, hashed at rest, expiring and revocable; they create no IAM identity. Preserve append-only review history and keep upload bodies and submission tokens out of logs.
- KYC uploads use the server-owned `KycDocuments:DocumentTypes` catalogue and an explicit named Other option. Preserve stable type keys, per-document upload controls on both collection channels, legacy classifications, private storage and classification-scoped deduplication.
- Persist first-administrator invitation intent separately from accounts. Use durable delivery leases, expiring hashed tokens, explicit acceptance and current-session authenticator proof before granting administrator access. Existing accounts authenticate using their established credentials. Ordinary IAM account invitation and recovery stay separate.
- Microsoft and Google sign-in bind a validated issuer and subject to the resolved workspace. Validate callback origins and protect workspace and intent in OIDC state. Consume hashed codes once in IAM and keep bearer/refresh tokens out of distributed cache.
- Keep MassTransit on the approved shared 8.x pin defined in `Directory.Packages.props` with exact central package ranges.
- Account responses and token claims are domain-neutral. Use AppUserMapping for backend account DTOs and LoginResponseMapping for UI login fallbacks; preserve tenant, session, permission and MFA state.
- Administrative endpoints require permission-based authorization in addition to `[Authorize]`.
- Resolve API authority from the live account, role memberships, direct grants, active permission definitions and module entitlements. Display role names never grant permissions. Tenant delegation is bounded by the actor's active grants, and protected administrator roles are maintained by trusted setup.
- Admin MFA enforcement is intentional; non-admin MFA can be policy-driven.
- Session lifecycle must expire cleanly, warn before timeout, and avoid stale UI state.
- Account access changes target the IAM account in the resolved tenant. Admin deactivation must atomically rotate the security stamp and revoke sessions/refresh tokens. Activation preserves credentials and email verification; invitation delivery is separate.
- Bearer access requires the signed tenant and session claims. First-factor challenges are accepted by the credential ceremony only. Forwarded headers are accepted from configured trusted proxies. On tenant-specific hosts, the hostname, any explicit selector and signed tenant must agree.
- Bind production data-plane hosts with Tenant:DeploymentStampId. An isolated stamp has one workspace, enforced through the persisted exclusive reservation. Its runtime has a separate signing key, issuer and audience. Setup and invitation delivery run on the owning stamp. Persist an expiring workspace setup lease, fence stale owners, and finish role initialization before delivering the invitation.
- Provisioning completion must match the persisted attempt, KYC and verified database schema/ownership. Machine callbacks use the dedicated GitHub OIDC policy with exact immutable repository identity, subject, ref, workflow and audience. Keep the operator permission endpoint separate. See docs/architecture/tenant-onboarding-workflow.md.
- Blazor tokens use protected browser storage with a circuit-scoped in-memory fallback; do not mirror bearer or refresh tokens into Redis.
- Profile images are private assets. Use API-mediated access, not direct public blob URLs.
- Local secrets belong in `dotnet user-secrets`; production secrets belong in Azure Key Vault/App Service configuration.
- Never commit secrets, connection strings, app passwords, tokens, private keys, or local temp files.

## Frontend Conventions

- Menu administration selects registered pages and searchable role/permission collections. Keep generated keys and descriptions out of the form. Select Module from available, implemented modules before selecting a registered Page; a catalog-owned page fixes its module. Preserve existing hidden values on edits. Menu visibility narrows navigation; it never grants route/API access.

- Menu icons use the shared MenuIconPicker for searchable installed MudBlazor Material styles, with result and selection previews. Access Catalog lists the full allowlist plus persisted custom labels; library-only definitions are not database records. Persist icon keys, never user-supplied SVG. Regenerate the shared backend allowlist with scripts/update-material-icon-keys.ps1 when upgrading MudBlazor.

- Authentication pages share AuthLayout and the compact centred auth-card composition, with AuthBrand inside the card. Publish sign-in controls only after workspace/provider initialization; preserve protected prerendering restrictions and avoid same-origin initialization reloads. Keep instructions task-specific; do not expose implementation explanations in product copy.
- Account actions live in the top-right header, with the user name and no role label. Sidebar navigation uses inset spacing and plain branding.

- The supported frontend is ST.UI.Blazor, a Blazor Web App with global Interactive Server rendering with prerendering disabled so protected browser tokens can be restored after the circuit starts. Clicks before the router is ready are held by wwwroot/js/appStartup.js until Routes sets data-app-ready. Keep that script ahead of blazor.web.js (docs/development/ui-conventions.md, Startup click gate).
- UI contracts belong in Features/{Context}/{Feature}/Contracts/Interfaces inside ST.UI.Blazor. Shared API DTOs remain in ST.SharedKernel.
- MAUI and a separate Razor Class Library are not part of this template.

- Blazor UI talks to the backend API boundary for app flows.
- Use MudBlazor components where practical and keep UI compact, responsive, and consistent.
- Dashboard action items require relevant operation authority as well as destination access. Distinguish personal actions, shared team queues and informational readiness; include the reason and next step without implying personal assignment.
- Operational tasks use persisted assignment/history with source occurrence keys and optimistic concurrency. Recheck an assignee's current workspace/account/MFA/effective permissions on every assignment; assignment never grants access. Claiming cannot steal another owner's task. Resolve through the underlying workflow. See docs/architecture/operational-task-assignment.md.
- Use `wwwroot/css` for app CSS.
- Avoid JavaScript unless Blazor cannot access the browser capability directly.
- Tables should support compact layout, horizontal overflow, pagination/search/filter patterns, and consistent actions.
- Administration listings follow the Access Catalog composition: one white card, a shared header with a neutral title and right-aligned Add action, optional Title Case tabs, filters below tabs and record counts in the pager. Embedded tab pages omit repeated titles and creation actions; their host owns the action. Keep tables white with subtle neutral header shading, and take action/status colors from MudProviderHost. See docs/development/ui-conventions.md.
- Keep View, Edit, Delete and More in fixed TableRowActions slots; unavailable actions remain disabled and empty More menus stay grey. Delete actions/confirmations use Error. Form submissions use green Success Save buttons with the Save icon; entry actions use Add. Button labels use Title Case. Dialog headers use neutral shading and status chips use subdued semantic tints with readable text.
- The account menu dismisses on navigation, click-away and Escape. Header context reflects the current account/workspace page, preserving support/Demo notices. Provider connection buttons have brand icons, spacing and responsive wrapping.
- New route guards must use native Blazor authorization: `@attribute [Authorize]`, `@layout` with a policy-protected layout, or `<AuthorizeView>`. Do not add new ad-hoc `HasPermission()` checks for route-level gating; leave existing `HasPermission()` calls for inline UI visibility unchanged.
- Control Plane pages must use `ControlPlaneLayout` and a native `[Authorize]` policy for their specific `PlatformPermissions` operation. The shared shell uses `ControlPlane.Access`; retired `control_plane.manage` grants no authority. AuthorizeRouteView enforces the page policy before page initialization. Layout attributes alone are not route authorization.

## Azure And Configuration

- Every settings POCO must have a matching JSON/config section.
- Parse configurable modes, providers, transports, and strategies once into typed values, then use `switch` branches with fail-fast unsupported-value errors.
- Keep technical configuration enums in the owning layer; reserve Domain enums for business language and invariants.
- Prefer Azure managed identity in deployed Azure environments.
- GitHub Actions authenticates to Azure through OIDC workload identity federation; do not add publish profiles or long-lived Azure client secrets to deployment workflows.
- Use connection strings only for local development or controlled non-managed-identity scenarios.
- Key Vault secret names use double hyphen mapping, for example `Section--Setting`.
- Environment variables use double underscore mapping, for example `Section__Setting`.
- Production email delivery must use approved provider API adapters. Personal mailbox SMTP is not a production path; Mailpit SMTP is local-only capture infrastructure.
- Keep Azure setup notes in `docs/development/azure-storage-configuration.md` and the checklist in `docs/development/environment-configuration-checklist.md`.
- Run application hosts from Visual Studio and local infrastructure through `ops/local/docker-compose.yml`; do not replace or remove Azure production provider configuration when adding local providers.

## Workflow Rules

- Preserve user changes. Do not revert unrelated work.
- Restore from the public-only repository nuget.config; no GitHub NuGet feed or token is required. GHCR container publishing is separate. Keep versions in Directory.Packages.props and project PackageReference items unversioned; per-project VersionOverride is disabled. Keep the SDK, target frameworks, ASP.NET/EF and approved Microsoft package families on stable 10.x. Run scripts/check-package-policy.ps1 for dependency changes and preserve the CI audit failures. Dependabot proposes reviewed patch/minor groups; MassTransit, Quartz and RazorStyle updates remain deliberate. See docs/development/package-management.md.
- Prefer small, focused commits with clear messages.
- When adding or modifying constructor dependencies for core abstractions (like `DbContext`), ensure the dependency injection configurations in the integration tests are also updated to provide those services.
- Before opening or updating a PR, run:
  - `dotnet build src\Backend\Api\ST.Api\ST.Api.csproj --no-restore -p:UseSharedCompilation=false`
  - `dotnet test tests\ST.Tests.Architecture\ST.Tests.Architecture.csproj --no-restore -p:UseSharedCompilation=false`
- Azure migrations/deployments run only when the GitHub repository variable AZURE_DEPLOYMENT_ENABLED is exactly true; false or missing pauses them for both main pushes and manual dispatch. Builds/tests and GHCR publishing remain available; ACR publishing is also paused. Avoid an environment variable of the same name overriding this repository switch.
- deploy-azure.yml triggers on main merges and manual dispatch; select app-service, aca-acr, or aca-ghcr in its manual dispatch input. Pushes use AZURE_DEPLOYMENT_TARGET.
- PRs should explain what changed, why, impact, and checks.
- If a review comment is valid and small, update the same PR branch.
- If a review comment changes scope materially, discuss before expanding the PR.

## Tool-Specific Entry Files

The following files exist only to point other AI tools back to this canonical file:

- `.github/copilot-instructions.md`
- `CLAUDE.md`
- `GEMINI.md`
- `.cursor/rules/starter-template.mdc`
- `.windsurfrules`

Keep these files short and aligned with this file. Do not let them become independent rulebooks.

## Local application isolation

- Each application host must have its own UserSecretsId; clones must regenerate identities and use distinct host ports and Compose project/volume names. The rename script and `scripts/allocate-local-ports.ps1` allocate a clone's ports automatically and record them in the per-user registry; local setup re-checks them. Identical rename reruns retain the generated identities. Infrastructure host ports live in the `*_HOST_PORT` values in the ignored local environment file. The setup scripts derive the SQL database name from the API prefix unless `LOCAL_DATABASE_NAME` is set.
- Run scripts/check-local-endpoints.ps1 before starting local infrastructure; supply new Compose files explicitly. Use -FocusSupplied to validate the proposed application against discovered reservations while reporting unrelated conflicts. Omit it for a full-workstation audit. See docs/development/workstation-endpoints.md.

- Local platform setup initializes missing optional payment fields through `scripts/initialize-payment-settings.ps1`. Preserve existing API user-secrets; keep provider credentials blank until supplied and record external-provider certification separately from core starter certification.

## Vendor and demo boundaries

- Reserved control_plane permissions require explicit platform_staff identity membership and the permission; tenant role names never grant platform authority.
- Reserved platform operation permissions are direct identity grants; tenant role claims cannot confer them. Keep issued claims, live API authority and current-user responses aligned. MFA enrollment guidance must include platform staff using custom role names.
- DevelopmentSeed:GrantPlatformAccess is opt-in for only the configured bootstrap identity and defaults false. Keep the local opt-in in user-secrets.
- IAM roles and catalogue uniqueness are tenant-scoped. Provider migrations must preserve ownership and reject ambiguous shared-role backfills.
- Module definitions may be admin-created, but activation depends on a deployed implementation registration. Tenant assignment uses the active, implemented catalogue.
- Demo is a reserved vendor-owned tenant. Its scoped proxy sessions carry no platform authority, preserve the vendor audit actor, and recheck vendor access. Keep demo tokens per browser tab, use normal MFA for vendor entry, and block uncertified outbound integrations.
- Support and Demo entry must switch the protected workspace selection together with scoped tokens. Save the platform selection/session and restore both on exit or recovery; never infer platform identity from whichever tenant token is currently available.
- Managed support uses customer-approved grants, explicit scope/duration, live vendor-authority checks and audited proxy sessions. Never substitute an audit-only impersonation record for a support session. Preserve the vendor actor, deny out-of-scope actions, enforce revocation/expiry on API requests and refresh, and retain a path back to the saved platform session.
- Module catalogue defaults are global; customer navigation and access customizations remain tenant-owned. Availability is the intersection of implementation, global lifecycle, dependencies, tenant entitlement and flags. Never copy a vendor catalogue to provision a customer.
- New module assignments with application-generated identifiers must be explicitly added through the ControlPlane Unit of Work repository; do not rely on EF attaching them through an already tracked tenant navigation collection.

- Control Panel is a permission-filtered navigation tile hub; its destination listings keep the shared table-card composition. Preserve former ?tab= bookmarks through redirects. Dashboard metrics come from authorized API data, exclude platform/Demo workspaces from customer counts and never imply unsupported lifecycle states. Sidebar order is Dashboard, workspace modules and Control Panel, with Admin Center at the bottom. Demo is a permission-protected Control Panel tool; retired overview/sample pages stay out of product navigation.
- Payments is a distinct module entitlement. Disabling or retiring a module must deny its APIs as well as hide navigation. Retire retains data and is terminal; Core/IAM are essential.

Ordinary people administration and credential entry require Human account kind; internal support/Demo proxies stay in their dedicated workflows. Interactive permission projection must preserve explicit supported platform grants independently of tenant-module unavailability.

Organization settings use OrganizationSettingCatalog: supported keys/defaults/options only, real consumers, audited revision-checked save/reset and tenant-aware cache invalidation. Presentation time zones never change TimeProvider UTC instants or security decisions. Use circuit-scoped PresentationPreferences for display; do not mutate process-wide culture for pooled tenants. See docs/development/organization-settings.md.

Tenant branding uses the existing tenant display name plus OrganizationBranding initials in auth and sidebar brand areas. Keep the initial shell neutral, fence explicit tenant-target branding, and use Organization Code/Change Organization for manual sign-in fallback. Public IsPlatformWorkspace is display metadata, never authorization. See docs/development/tenant-branding-and-sign-in.md.

## Company frontend routing

Company app origins are explicitly registered, canonical, unique and tenant-bound before restoring UI sessions. Shared frontend origins use `Tenant:CentralFrontendOrigins`. Preserve `X-Frontend-Origin` propagation for JSON/uploads/downloads, fail-closed mismatches and JWT tenant enforcement. This header and public branding flags never grant authority. SSO return targets must match the requested active tenant or an approved shared origin; keep the exact callback route. See `docs/development/tenant-branding-and-sign-in.md`.
