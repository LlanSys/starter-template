# Database Migrations Workflow

StarterTemplate supports multiple database providers simultaneously (e.g., SQL Server and PostgreSQL) within a modular monolith architecture. This means each bounded context (IAM, Banking, HR, Shared, etc.) has its own set of migrations, and each set of migrations must be generated for each supported provider.

## Core Principles

1. **Never use the base `DbContext` for migrations.**
   The base `DbContext` classes (e.g., `SharedDBContext`, `IamDBContext`) contain the `DbSet` properties and business logic, but they are NOT used to generate migrations.
2. **Always use the Provider-Specific derived `DbContext`.**
   Migrations MUST be generated using the provider-specific contexts (e.g., `SharedSqlServerDBContext` and `SharedPostgreSqlDBContext`).
3. **Always output to the Provider-Specific folder.**
   SQL Server migrations go to `Features/{Context}/Migrations/SqlServer`.
   PostgreSQL migrations go to `Features/{Context}/Migrations/PostgreSql`.

## How to generate a Migration

Whenever you add a new property, column, class, or make any schema changes, you **must run a migration for EVERY supported provider**.

Use the following commands from the repository root:

### SQL Server
```powershell
dotnet ef migrations add <MigrationName> `
  --context <ContextName>SqlServerDBContext `
  --output-dir Features/<ContextName>/Migrations/SqlServer `
  --project src\Backend\Persistence\ST.Persistence\ST.Persistence.csproj `
  --startup-project src\Backend\Api\ST.Api\ST.Api.csproj
```

### PostgreSQL
```powershell
dotnet ef migrations add <MigrationName> `
  --context <ContextName>PostgreSqlDBContext `
  --output-dir Features/<ContextName>/Migrations/PostgreSql `
  --project src\Backend\Persistence\ST.Persistence\ST.Persistence.csproj `
  --startup-project src\Backend\Api\ST.Api\ST.Api.csproj
```

**Example:** Adding a new property to a Shared feature:
```powershell
dotnet ef migrations add AddNewProperty `
  --context SharedSqlServerDBContext `
  --output-dir Features/Shared/Migrations/SqlServer `
  --project src\Backend\Persistence\ST.Persistence\ST.Persistence.csproj `
  --startup-project src\Backend\Api\ST.Api\ST.Api.csproj

dotnet ef migrations add AddNewProperty `
  --context SharedPostgreSqlDBContext `
  --output-dir Features/Shared/Migrations/PostgreSql `
  --project src\Backend\Persistence\ST.Persistence\ST.Persistence.csproj `
  --startup-project src\Backend\Api\ST.Api\ST.Api.csproj
```

## Applying Migrations in CI/CD
The GitHub Actions workflow `deploy-azure.yml` uses the `efbundle` tool to apply migrations dynamically depending on the selected provider. It executes a bundle per bounded context (e.g., `efbundle-iam`, `efbundle-shared`).

If you encounter a `SqlException: Cannot find the object` error in the pipeline, it usually means:
- The migration was accidentally generated against the base `DbContext` rather than the provider-specific `DbContext`.
- An earlier migration was deleted or skipped.
- The snapshot is out of sync.
Always verify that your migrations exist in the correct provider folder (e.g., `Features/Shared/Migrations/SqlServer`).
