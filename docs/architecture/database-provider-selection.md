# Database Provider Selection

StarterTemplate supports SQL Server and PostgreSQL side by side. A downstream application is a one-time fork that normally runs on one database. It chooses that database when it renames the template, and the other provider is removed completely: its DbContexts, migrations, SQL generators, health check, tests, packages, Bicep modules, CI branches and local-platform services.

## Why choose at fork time

Keeping both providers costs something on every change, forever:

- Every schema change needs two migrations.
- Integration tests run twice.
- Shared code needs provider branches.
- CI and infrastructure carry both paths.

Today a deployment runs one provider anyway. `TenantConnectionResolver` rejects a tenant whose provider differs from the deployment's `DatabaseSettings:Provider`, so tenants can't run on mixed providers within one deployment. Moving an existing tenant to another provider is a data migration project in either case: data types, collation, concurrency tokens and identity values all differ. Keeping both providers' code only saves you generating the target schema, and the template still holds that if you need it.

Choose `Both` only when the product itself must run on either database, for example when customers host it themselves or enterprise buyers require a specific database engine.

## Usage

```powershell
.\scripts\rename-template.ps1 -NewName MyApp -NamespacePrefix MA -DatabaseProvider PostgreSql -ResetGitHistory -WhatIf
.\scripts\rename-template.ps1 -NewName MyApp -NamespacePrefix MA -DatabaseProvider PostgreSql -ResetGitHistory
```

| Parameter | Effect |
| --- | --- |
| `-DatabaseProvider SqlServer` / `PostgreSql` | Removes the other provider. Required. |
| `-DatabaseProvider Both` | Keeps today's dual-provider setup, including markers. |
| `-ResetGitHistory` | Checks for a clean working tree and that `origin` is the template, deletes `.git` first, then after the rename runs `git init -b main` and makes one initial commit. It adds no remote. |

The script order is: preflight, delete git history, select the provider (while paths still use the template prefix), rename the identity, initialise the fresh repository.

The choice is recorded in `scripts/database-providers.json` (`selectedProvider`). Rerunning with the same provider does nothing. Rerunning with a different provider is refused: re-clone the template instead.

## How removal works

`scripts/select-database-provider.ps1` reads `scripts/database-providers.json` and runs these steps:

1. **Files.** Deletes everything matching the removed provider's `paths` globs: derived DbContexts and design-time factories, `Migrations/<Provider>` folders, the idempotent SQL generator, the health check, provider-named integration tests and fixtures, and the Bicep module.
2. **Packages.** Removes the provider's `PackageVersion` / `PackageReference` lines.
3. **Markers.** Drops the removed provider's marker blocks from shared files and strips every marker line.
4. **Configuration.** Applies the kept provider's `keepReplacements`, such as `DatabaseSettings:Provider` and `DatabaseSettings.DefaultProvider`.
5. **Tooling.** Deletes `templateOnly` tooling (the fork-check workflow and script). For a single-provider fork it also deletes `markerTooling` (the marker guardrail test).
6. **Leftover scan.** Fails if any non-Markdown text file or path still matches the removed provider's `leftoverPattern`.

### Marker convention

Wrap provider-specific lines in shared files in whole-line comment markers. Blocks cannot nest, and unbalanced or unknown markers stop the script.

```csharp
// <db:PostgreSql>
if (dbSettings.Is(DatabaseProviders.PostgreSql))
{
    options.UseNpgsql(connectionString);
}
// </db:PostgreSql>
// <db:SqlServer>
if (dbSettings.Is(DatabaseProviders.SqlServer))
{
    options.UseSqlServer(connectionString);
}
// </db:SqlServer>
```

Comment forms: `//` (C#, Bicep), `#` (YAML, PowerShell, shell, `.env`), `<!-- -->` (XML, Markdown), `--` / `REM`.

Rules for new code:

- Write each provider's branch as an independent `if` block, not as `if/else`. Each fork must then compile and behave correctly with only one block left.
- Put provider-specific `using` directives, exception filters (`SqlException`, `PostgresException`) and package references inside markers.
- Compare provider names with `ST.Domain.Shared.DatabaseProviders` and `DatabaseSettings.Is(...)`. `DatabaseSettings.EnsureSupported()` makes startup fail when configuration names a provider the build doesn't contain.
- Give a new provider-specific file a provider name that matches the manifest globs, such as `*SqlServer*.cs` or `*PostgreSql*.cs`, or add its path to the manifest.
- Keep Markdown prose neutral where possible. Mark provider-specific doc sections with `<!-- <db:X> -->`. Markdown is not part of the leftover scan.

### Guardrails in the template

- `tests/ST.Tests.Architecture/Template/ProviderMarkerGuardrailTests.cs` checks that provider API usage outside manifest-deleted files sits inside a matching marker, and that markers are balanced.
- `scripts/test-rename-template.ps1` runs the rename and selection on a fixture, including marker errors, leftovers, `Both` and git reset.
- `scripts/test-database-provider-selection.ps1` and `.github/workflows/template-fork-check.yml` fork the real template for each provider, then build it and run unit and architecture tests. The scheduled nightly run also runs the integration suite.

## Switching provider after the fork

A fork contains one provider. To move to the other provider later:

1. Copy the target provider's files from the same StarterTemplate revision: derived contexts, factories, generator, health check, test fixtures, Bicep module and packages. Reinstate its marker blocks from the template's shared files.
2. Generate one fresh initial migration per context for the target provider from the current model. Don't port the template's history.
3. Move the data with a tool such as pgloader, or with an ETL. Check data types (`datetimeoffset` vs `timestamptz`, `uniqueidentifier`, `nvarchar` vs `text`), case-sensitivity and collation, concurrency tokens (`rowversion` vs the stamped byte token), identity and sequence values, and null ordering.
4. Rehearse the cutover on a copy, reconcile row counts and checksums per table, then switch `DatabaseSettings:Provider` and the connection strings.

## Future alternative: dialect classes

Markers are a deliberate first step. A later refactor could move every provider branch behind small provider classes, for example an `ISqlDialect` with `SqlServerDialect` / `PostgreSqlDialect` implementations covering uniqueness-violation detection, concurrency stamping, outbox and scheduler registration, readiness checks and DbContext options. Provider selection would then only delete files, and shared code would need no markers. Consider it if the marker count grows or reviewers find marked code hard to read.
