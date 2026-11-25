# StarterTemplate

> Current work: [domain-neutral refactor plan](docs/architecture/domain-neutral-refactor-plan.md). Clone setup and verification limits: [starter-setup.md](docs/development/starter-setup.md). StarterTemplate is self-contained; no separate repository or extraction revision is required.

StarterTemplate is a reusable enterprise .NET template for building production-minded systems from a modular monolith starting point.

It is designed to be cloned, renamed, configured, and extended into a real application without redoing the core plumbing every time.

## What Is Included

- `.NET 10` backend with Clean Architecture boundaries.
- Modular monolith structure with bounded-context feature folders.
- Current bounded contexts: `IAM`, `Shared`, and `ControlPlane`.
- API, Blazor Web UI (Interactive Server), Application, Domain, Infrastructure, Persistence, and SharedKernel projects.
- IAM/Auth baseline with local accounts, JWT/cookie-compatible flows, refresh tokens, MFA/TOTP, sessions, lockout, permission authorization, grant/revoke access, profile image upload, and inactivity handling.
- EF Core persistence with per-context `DBContext` types, tenant filters, soft delete, audit stamping, explicit configurations, repositories, unit of work, and migration bundles.
- MassTransit with EF outbox, RabbitMQ local transport, and Azure Service Bus production transport configuration.
- Local Docker platform for RabbitMQ, Redis, Mailpit, Azurite, optional Seq, and optional SQL Server.
- Output caching, response compression, rate limiting, health checks, feature flags, SignalR baseline, QuestPDF reporting abstraction, and payment gateway abstraction.
- Architecture tests and naming/convention guardrails that run in CI.

## Clone And Rename

Clone the template from GitHub, then rename it before doing anything else in the new folder. Run with `-WhatIf` first to preview the changes:

```powershell
git clone https://github.com/LlanSys/starter-template.git E:\Repos\InsurHub
cd E:\Repos\InsurHub
.\scripts\rename-template.ps1 -NewName InsurHub -NamespacePrefix IH -DatabaseProvider PostgreSql -ResetGitHistory -WhatIf
.\scripts\rename-template.ps1 -NewName InsurHub -NamespacePrefix IH -DatabaseProvider PostgreSql -ResetGitHistory
git remote add origin <new-remote-url>
git push -u origin main
```

This produces names such as `IH.Domain`, `IH.Application`, and `IH.Api`.

- `-DatabaseProvider` is required. `SqlServer` or `PostgreSql` keeps only that provider; `Both` keeps both. See [database provider selection](docs/architecture/database-provider-selection.md).
- `-ResetGitHistory` replaces the template's git history with a fresh repository that has one initial commit, ready for the new remote.

Clone from GitHub rather than copying a local StarterTemplate folder. A copy can bring along branches, uncommitted changes, build output and local `.env` values, and `-ResetGitHistory` refuses to run when there are uncommitted changes.

Then work through the [downstream configuration checklist](docs/development/downstream-configuration-checklist.md).

Read [Renaming the template](docs/development/renaming-the-template.md) before cloning for a real product, especially the `DataProtection:ApplicationName` warning.

## Local Development Quick Start

1. Start Docker Desktop.
2. Run the local platform setup from the repository root:

```powershell
.\scripts\setup-local-platform.ps1
```

3. Configure SQL Server connection and required secrets with `dotnet user-secrets`.
4. Run `ST.Api` and `ST.UI.Blazor` from Visual Studio.
5. Open Mailpit at `http://localhost:8030` to inspect captured local emails.

The local setup uses:

- RabbitMQ for messaging.
- Redis for distributed cache.
- Mailpit for email capture.
- Azurite for Blob-compatible profile image storage.
- Local SQL Server or optional SQL Server container.

See [Local Platform](docs/development/local-platform.md) and [Environment Configuration Checklist](docs/development/environment-configuration-checklist.md).

## Verification

Before opening a PR, run:

```powershell
dotnet build src\Backend\Api\ST.Api\ST.Api.csproj --no-restore -p:UseSharedCompilation=false
dotnet test tests\ST.Tests.Architecture\ST.Tests.Architecture.csproj --no-restore -p:UseSharedCompilation=false
```

Useful additional checks:

```powershell
dotnet test tests/ST.Tests.Unit/ST.Tests.Unit.csproj --no-restore -p:UseSharedCompilation=false
.\scripts\test-local-messaging.ps1
```

## Azure And Production Configuration

Azure deployment support is present but intentionally gated.

- Keep local development on user-secrets plus Docker services.
- Keep production secrets in Azure Key Vault or App Service settings.
- Prefer managed identity for Azure Blob, Key Vault, Redis, and App Service integration.
- Enable deployment only when the GitHub repository variable `AZURE_DEPLOYMENT_ENABLED` is set to `true`.

See:

- [Azure Storage Configuration](docs/development/azure-storage-configuration.md)
- [Azure CI/CD Configuration](docs/development/azure-cicd-configuration.md)
- [CI/CD Workflow](docs/development/ci-cd-workflow.md)
- [Deployment Options](docs/development/deployment-options.md)

## Documentation Map

- [Architecture Plan](PLAN.md)
- [Execution Strategy](PLAN_EXECUTION_STRATEGY.md)
- [Feature Folder Convention](docs/architecture/feature-folder-convention.md)
- [Persistence Standards](docs/architecture/persistence-standards.md)
- [UI To Backend Flow](docs/architecture/ui-to-backend-flow.md)
- [Configuration Code Conventions](docs/development/configuration-code-conventions.md)
- [Deployment Options](docs/development/deployment-options.md)
- [Pull Request Guidelines](docs/development/pull-request-guidelines.md)

## Clone Readiness

The core starter is certified for cloning: the domain-neutral refactor is complete (see the [refactor plan](docs/architecture/domain-neutral-refactor-plan.md)), and every PR forks the template once per database provider to prove the result builds and passes its tests. To configure a clone:

1. Clone from GitHub and rename before the first run (see [Clone And Rename](#clone-and-rename)).
2. Configure local secrets and the database connection for the chosen provider.
3. Run the local Docker platform.
4. Keep product-specific modules in the clone, not in the base template.

Some provider-dependent capabilities still need environment-specific certification in the target application:

- Entra ID AppUser linking flow.
- Passkeys/WebAuthn.
- Real Stripe/M-Pesa credentials, callbacks, idempotency, and reconciliation.
- Azure Service Bus smoke test.
- Azure deployment smoke test after a valid Azure subscription is available.

These are tracked in [PLAN.md](PLAN.md), so downstream projects can decide whether to complete them in the template first or activate them after cloning.
