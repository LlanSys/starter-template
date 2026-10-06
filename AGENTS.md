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

- Repo: `odhiamboally/starter-template`
- Product: StarterTemplate
- Root namespace prefix: `ST`
- Rename a clone with `scripts/rename-template.ps1` before its first run or deployment.
- After protected data has been issued, `DataProtection:ApplicationName` is immutable without an explicit migration or security reset.
- Target framework: `.NET 10`
- Architecture: modular monolith with Clean Architecture boundaries and bounded-context feature folders.
- Retained platform contexts: `IAM`, `Shared`, and `ControlPlane`. `Banking` and `HR` are temporarily retained while IAM is decoupled, then removed by the domain-neutral refactor plan.

## Architecture Rules

- Dependency direction is inward: `Domain <- Application <- Infrastructure/Persistence <- Api/UI`.
- `Domain` must not reference Application, Infrastructure, Persistence, API, or UI.
- `Application` must not reference Infrastructure or Persistence.
- `SharedKernel` must not reference Domain, Infrastructure, or Persistence.
- Infrastructure and Persistence implement contracts; they do not define business policy.
- Keep feature-owned code inside `Features/{Context}/{Feature}` in every layer where that code exists.
- Keep cross-cutting concerns outside feature folders: logging, middleware, generic caching, generic repositories, JSON helpers, base entities, and shared abstractions.

## Backend Conventions

- Use MediatR request/handler flows already established in the repo.
- Commands/queries and handlers should remain feature-owned.
- Each public top-level type must live in its own file named after that type. This applies to classes, records, structs, interfaces, and enums; do not bundle multiple public DTOs, validators, settings POCOs, entities, or helper types in one file.
- Use `AppResponse<T>` for expected business outcomes.
- Throw exceptions only for unexpected or exceptional failures.
- API responses must go through the established response/problem-details pattern.
- Do not leak exception type names, stack traces, provider errors, connection strings, or internal IDs into user-facing messages.
- All user-facing errors from the UI must be sanitized through the shared messaging pattern.
- Provider-specific integration DTOs belong beside the provider adapter. Shared contracts should stay provider-neutral and should not expose Stripe, M-Pesa, Azure, or other provider wire payloads.
- When a capability supports multiple runtime providers per operation, use a router/factory over isolated provider adapters. Do not make one provider adapter understand another provider's DTOs, credentials, or callbacks.
- Application-layer command/query handlers must use bounded-context Unit of Work interfaces (`ISharedUnitOfWork`, `IBankingUnitOfWork`, etc.) for persistence orchestration, not raw `IRepository<T>`. Always call `CompleteAsync` with a `CancellationToken` after write operations.
- Pass `CancellationToken` to all async I/O calls (`ReadAsStringAsync`, `CompleteAsync`, `SaveChangesAsync`, etc.) where the parameter is available.
- Use `StringComparison.OrdinalIgnoreCase` for case-insensitive comparisons. Reserve `ToUpperInvariant()`/`ToLowerInvariant()` for value normalization (stored keys, wire formats, display strings) — never for equality checks. C# `switch` on normalized strings is acceptable when `StringComparison` is not supported by the expression.
- Always use `Guid.CreateVersion7()` instead of `Guid.NewGuid()` to ensure time-ordered identifiers.

## Persistence Rules

- Every declared `DbSet<T>` entity must have explicit EF configuration.
- Custom persistence context-related types use the project `DB` acronym, for example `IamDBContext`, `DBContextHelper`, and `ITenantFilteredDBContext`; keep Microsoft framework API names unchanged.
- Respect tenant isolation, soft delete, and audit actor conventions.
- `CreatedBy`, `UpdatedBy`, `ActivatedBy`, `DeactivatedBy`, and `DeletedBy` must store stable actor identifiers, not display names or labels such as `DevelopmentSeed`.
- Prefer generic repository methods unless a concrete repository method is persistence-specific.
- Compose queries before materialization. Do not call `ToListAsync` before filters, tenant scope, paging, projection, or security scope are applied.
- Use `Any` for existence checks, not `Count > 0`.
- Use `CountAsync` only when the count is returned, logged, or used for a decision.
- Avoid N+1 query patterns. Batch identifiers and map results in memory.
- Cursor pagination must fetch `pageSize + 1` before trimming.
- **StarterTemplate supports multiple database providers (PostgreSQL and SQL Server).** When adding EF Core migrations, you MUST generate separate migrations for each provider's DbContext and specify the correct output directory. For example, for the IAM bounded context: `dotnet ef migrations add <Name> -c IamSqlServerDBContext -o Migrations/IAM` and `dotnet ef migrations add <Name> -c IamPostgreSqlDBContext -o Migrations/IamPostgreSqlDB`. Do not rely on default output directories.
- **Database Context Factories:** When creating `IDesignTimeDbContextFactory<T>` implementations, always specify a provider-specific fallback connection string using `DesignTimeConfigurationFactory.GetConnectionString(..., fallbackConnectionName: "DefaultPostgreSqlConnection")` (or `"DefaultSqlConnection"`) to prevent parsing crashes from incompatible connection string parameters.
- **Idempotent Migrations:** EF Core index creations are globally intercepted by custom SQL generators (`IdempotentSqlServerMigrationsSqlGenerator` and `IdempotentNpgsqlMigrationsSqlGenerator`) to inject `IF NOT EXISTS` logic. Do not write manual raw SQL for idempotency on index creation—the generator handles this automatically.

## Validation, Logging, And Caching

- Add FluentValidation validators for write requests that can accept user input.
- Use source-generated `LoggerMessage` methods; do not add free-form logging in new code paths.
- Every `catch` block must log or deliberately translate the error into an expected result.
- Use `ICachableRequest` for cacheable queries and `ICacheInvalidatorRequest` for commands that invalidate cache.
- Keep cache keys tenant-aware where data is tenant-scoped.

## IAM And Security

- IAM is a core platform capability, not an application-specific feature.
- Administrative endpoints require permission-based authorization, not only `[Authorize]`.
- Admin MFA enforcement is intentional; non-admin MFA can be policy-driven.
- Session lifecycle must expire cleanly, warn before timeout, and avoid stale UI state.
- Blazor tokens use protected browser storage with a circuit-scoped in-memory fallback; do not mirror bearer or refresh tokens into Redis.
- Profile images are private assets. Use API-mediated access, not direct public blob URLs.
- Local secrets belong in `dotnet user-secrets`; production secrets belong in Azure Key Vault/App Service configuration.
- Never commit secrets, connection strings, app passwords, tokens, private keys, or local temp files.

## Frontend Conventions

- Blazor UI talks to the backend API boundary for app flows.
- Use MudBlazor components where practical and keep UI compact, responsive, and consistent.
- Use `wwwroot/css` for app CSS.
- Avoid JavaScript unless Blazor cannot access the browser capability directly.
- Tables should support compact layout, horizontal overflow, pagination/search/filter patterns, and consistent actions.
- Listing pages follow the shared compact layout: create actions occupy a separate top-right page action row, shown-count badges occupy the list heading's right edge, and search/filter controls occupy a full-width row below the heading.
- New route guards must use native Blazor authorization: `@attribute [Authorize]`, `@layout` with a policy-protected layout, or `<AuthorizeView>`. Do not add new ad-hoc `HasPermission()` checks for route-level gating; leave existing `HasPermission()` calls for inline UI visibility unchanged.
- Control Plane pages must use `ControlPlaneLayout` (`@layout ControlPlaneLayout`), which enforces the `ControlPlane.Manage` policy at the layout level before `OnInitializedAsync` fires.

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

- Each application host must have its own UserSecretsId; clones must regenerate identities and allocate distinct host ports and Compose project/volume names.
- Run scripts/check-local-endpoints.ps1 before starting local infrastructure; supply new Compose files explicitly. See docs/development/workstation-endpoints.md.
