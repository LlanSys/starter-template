# Database Migrations Workflow

Each bounded context (IAM, Shared, ControlPlane and any downstream contexts) owns its own migrations and migration history table. The template ships SQL Server and PostgreSQL migrations side by side. A fork keeps only the provider chosen at rename time; see [database provider selection](../architecture/database-provider-selection.md). Every provider kept in the repository needs a migration for every schema change.

## Core Principles

1. **Never use the base `DbContext` for migrations.**
   The base `DbContext` classes (e.g., `SharedDBContext`, `IamDBContext`) contain the `DbSet` properties and business logic, but they are NOT used to generate migrations.
2. **Always use the provider-specific derived `DbContext`.**
   Migrations MUST be generated using the provider-specific contexts listed below.
3. **Always output to the provider-specific folder.**
<!-- <db:SqlServer> -->
   SQL Server migrations go to `Features/{Context}/Migrations/SqlServer`.
<!-- </db:SqlServer> -->
<!-- <db:PostgreSql> -->
   PostgreSQL migrations go to `Features/{Context}/Migrations/PostgreSql`.
<!-- </db:PostgreSql> -->

## How to generate a Migration

Whenever you add a new property, column, class, or make any schema changes, run the command below for every provider present in the repository, from the repository root.

<!-- <db:SqlServer> -->
### SQL Server
```powershell
dotnet ef migrations add <MigrationName> `
  --context <ContextName>SqlServerDBContext `
  --output-dir Features/<ContextName>/Migrations/SqlServer `
  --project src\Backend\Persistence\ST.Persistence\ST.Persistence.csproj `
  --startup-project src\Backend\Api\ST.Api\ST.Api.csproj
```

Example, a new property in a Shared feature:
```powershell
dotnet ef migrations add AddNewProperty `
  --context SharedSqlServerDBContext `
  --output-dir Features/Shared/Migrations/SqlServer `
  --project src\Backend\Persistence\ST.Persistence\ST.Persistence.csproj `
  --startup-project src\Backend\Api\ST.Api\ST.Api.csproj
```
<!-- </db:SqlServer> -->

<!-- <db:PostgreSql> -->
### PostgreSQL
```powershell
dotnet ef migrations add <MigrationName> `
  --context <ContextName>PostgreSqlDBContext `
  --output-dir Features/<ContextName>/Migrations/PostgreSql `
  --project src\Backend\Persistence\ST.Persistence\ST.Persistence.csproj `
  --startup-project src\Backend\Api\ST.Api\ST.Api.csproj
```

Example, a new property in a Shared feature:
```powershell
dotnet ef migrations add AddNewProperty `
  --context SharedPostgreSqlDBContext `
  --output-dir Features/Shared/Migrations/PostgreSql `
  --project src\Backend\Persistence\ST.Persistence\ST.Persistence.csproj `
  --startup-project src\Backend\Api\ST.Api\ST.Api.csproj
```
<!-- </db:PostgreSql> -->

## Applying Migrations in CI/CD
The GitHub Actions workflow `deploy-azure.yml` uses the `efbundle` tool to apply migrations for the configured provider. It executes a bundle per bounded context (e.g., `efbundle-iam`, `efbundle-shared`).

If the pipeline reports a missing table or object, it usually means:
- The migration was accidentally generated against the base `DbContext` rather than the provider-specific `DbContext`.
- An earlier migration was deleted or skipped.
- The snapshot is out of sync.
Always verify that your migrations exist in the correct provider folder (e.g., `Features/Shared/Migrations/<Provider>`).
