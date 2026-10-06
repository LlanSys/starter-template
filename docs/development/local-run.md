# Local Run Guide

This guide is for running the solution locally from Visual Studio with repeatable Docker-hosted infrastructure dependencies.

## Current Local Strategy

Run application code from Visual Studio:

- `ST.Api`
- `ST.UI.Blazor`
- EF migrations and debugging

Run RabbitMQ, Redis, Mailpit, and Azurite through `ops/local/docker-compose.yml`. SQL Server and Seq remain optional Compose profiles because developers may already have them installed.

See [Local Platform](local-platform.md) and run:

```powershell
./scripts/setup-local-platform.ps1
```

## Development Toggles

The Development environment disables infrastructure that should not block basic local API/UI work:

- `Messaging:Enabled = true`
- `BackgroundJobs:Enabled = false`
- `Observability:Enabled = false`
- `DevelopmentSeed:Enabled = true`

Production defaults keep these capabilities enabled unless explicitly configured otherwise.

## Messaging Transport

Development uses RabbitMQ. Azure production uses Azure Service Bus. Both use the same MassTransit consumers and EF outbox registration. StarterTemplate pins Apache-2.0-licensed MassTransit `8.5.10`, which targets .NET 10; the commercial v9 dependency was intentionally removed from the reusable template.

## Required User Secrets

Set local secrets on the API project:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=BT;Trusted_Connection=True;TrustServerCertificate=True;" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "JwtSettings:SecretKey" "LOCAL-DEVELOPMENT-ONLY-CHANGE-ME-TO-A-LONG-RANDOM-SECRET-AT-LEAST-32-CHARS" --project src\Backend\Api\ST.Api\ST.Api.csproj
```

Adjust the SQL Server name if your instance is different, for example:

```powershell
Server=.\SQLEXPRESS;Database=BT;Trusted_Connection=True;TrustServerCertificate=True;
```

## Apply Local Database Schema

Run the EF migrations once after setting the connection string:

```powershell
dotnet ef database update --context IamDBContext --project src\Backend\Persistence\ST.Persistence\ST.Persistence.csproj --startup-project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet ef database update --context HrDBContext --project src\Backend\Persistence\ST.Persistence\ST.Persistence.csproj --startup-project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet ef database update --context SharedDBContext --project src\Backend\Persistence\ST.Persistence\ST.Persistence.csproj --startup-project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet ef database update --context BankingDBContext --project src\Backend\Persistence\ST.Persistence\ST.Persistence.csproj --startup-project src\Backend\Api\ST.Api\ST.Api.csproj
```

The bounded contexts share the same local database, but each context has its own migration history table.

## Development Admin

When `ST.Api` starts in Development, it seeds Identity roles and this local admin user:

- Username: `aamodhiambo@gmail.com`
- Password: `Admin@12345`
- Role: `System Administrator`

This seed is disabled outside Development.

## Run From Visual Studio

1. Open the solution.
2. Configure multiple startup projects: `ST.Api` and `ST.UI.Blazor`.
3. Start both projects with the `https` profiles.
4. Browse API documentation at `https://localhost:7129/scalar/v1`.
5. Browse the Blazor UI at `https://localhost:7049`.
6. Sign in through `/iam/sign-in` with the development admin account.
7. Use `/iam/security` to view the current user and configure authenticator-app two-factor authentication.

The Blazor app calls the API through `BackendApi:BaseUrl`. In Development this is configured as:

```json
{
  "BackendApi": {
    "BaseUrl": "https://localhost:7129/"
  }
}
```

The UI must not reference backend Application, Domain, MediatR handlers, EF Core contexts, or repositories directly. Feature UI flows should call API endpoints through typed HTTP client services and shared DTO contracts.

See `docs/architecture/ui-to-backend-flow.md` for the rule of thumb and the tradeoff behind this decision.

## Current Known Local Gaps

- Quartz tables are not created yet, so background jobs stay disabled in Development.
- The MediatR package currently emits a development/test license warning. This is not blocking local development, but production licensing or replacement must be decided later.
