# Non-Azure Deployment Configuration

This guide covers the non-Azure deployment paths for StarterTemplate and cloned products. Azure remains the preferred enterprise path when the subscription is active, but the template also supports container deployment to DigitalOcean, Heroku, and generic Docker hosts.

## Platform Equivalents

DigitalOcean and Heroku do not provide exact one-to-one replacements for every Azure service. The production-safe pattern is to keep the application provider-neutral and configure each host through environment variables, managed add-ons, or external services.

| Concern | Azure | DigitalOcean | Heroku | StarterTemplate setting |
| --- | --- | --- | --- | --- |
| App hosting | App Service or Container Apps | App Platform or Droplet | Dyno / Container Runtime | CI/CD target |
| Container registry | ACR, GHCR, Docker Hub | DOCR, GHCR, Docker Hub | Heroku Container Registry | Workflow-specific |
| Secrets | Key Vault + App Service settings | App Platform encrypted env vars or external vault | Config Vars or external vault | `Section__Setting` env vars |
| TLS certificates | App Service managed certs or uploaded certs | App Platform managed certs / load balancer certs | Automated Certificate Management | Platform-managed |
| Redis | Azure Cache for Redis | DigitalOcean Managed Redis | Heroku Redis / Key-Value Store | `CacheSettings__Provider` |
| Object storage | Azure Blob Storage | DigitalOcean Spaces or S3-compatible storage | External S3-compatible storage | `ProfileImageStorage__Provider` |
| Data Protection keys | Azure Blob + Key Vault | Redis key ring or private persistent volume | Redis key ring or external persistent store | `DataProtection__*` |
| Messaging | Azure Service Bus or RabbitMQ | RabbitMQ provider or self-hosted RabbitMQ | RabbitMQ provider or external RabbitMQ | `Messaging__Transport` |
| Database | Azure SQL | SQL Server-compatible external host or managed VM | External SQL Server-compatible host | `ConnectionStrings__DefaultConnection` |
| Email | SendGrid / provider API | SendGrid / provider API | SendGrid / provider API | `EmailSettings__Provider` |

## Current Non-Azure Workflow

The workflow is `.github/workflows/non-azure-deploy.yml`.

It supports manual dispatch with:

```text
deploy_target=digitalocean-app-platform
deploy_target=heroku-container
deploy_target=generic-docker
```

### DigitalOcean App Platform

Flow:

```text
VS -> GitHub Repo -> GitHub Actions -> GHCR image -> DigitalOcean App Platform deployment
```

Required GitHub configuration:

| Name | Type | Purpose |
| --- | --- | --- |
| `DIGITALOCEAN_ACCESS_TOKEN` | Secret | Allows GitHub Actions to call `doctl` |
| `DIGITALOCEAN_APP_ID` | Variable | The existing App Platform app to redeploy |

Required DigitalOcean setup:

- Create the App Platform app once.
- Configure the API and Blazor components to use the GHCR images.
- Configure runtime environment variables in App Platform.
- Configure the API component health check to `/health/ready`.
- Configure private services for Redis, RabbitMQ, SQL Server-compatible database, and object storage as needed.

DigitalOcean App Platform can pull from GHCR, but the image/package visibility and credentials must be configured in DigitalOcean if the GHCR package is private.

### Heroku Container Runtime

Flow:

```text
VS -> GitHub Repo -> GitHub Actions -> Docker image -> Heroku Registry -> Heroku Release
```

Required GitHub configuration:

| Name | Type | Purpose |
| --- | --- | --- |
| `HEROKU_API_KEY` | Secret | Authenticates Docker registry push and Heroku release |
| `HEROKU_API_APP_NAME` | Variable | Heroku app name for `ST.Api` |
| `HEROKU_BLAZOR_APP_NAME` | Variable | Heroku app name for `ST.UI.Blazor` |

Required Heroku setup:

- Create two Heroku apps: one for the API and one for Blazor.
- Configure Heroku Config Vars for each app.
- Configure external SQL Server-compatible database connectivity.
- Configure external Redis/RabbitMQ/object storage providers.
- Ensure the Blazor `BackendApi__BaseUrl` points to the deployed API URL.

StarterTemplate reads Heroku's assigned `PORT` environment variable at startup. This is required because Heroku does not guarantee a fixed container port.

### Generic Docker Host

Flow:

```text
VS -> GitHub Repo -> GitHub Actions -> GHCR image -> VPS Docker Compose
```

The `generic-docker` target publishes GHCR images and prints the image references. The host is responsible for:

- Pulling images.
- Running Compose or a container orchestrator.
- Terminating TLS.
- Configuring secrets.
- Running migrations.
- Backups and monitoring.

This option is flexible but operationally heavier than managed App Platform or Heroku.

## Runtime Configuration On Non-Azure Hosts

Use environment variables with double underscores.

Minimum production-style keys:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<sql-server-connection>
JwtSettings__Secret=<strong-secret>
BackendApi__BaseUrl=https://<api-host>/
AllowedOrigins__0=https://<blazor-host>
EmailSettings__Provider=SendGrid
EmailSettings__SendGrid__ApiKey=<sendgrid-key>
EmailSettings__FromAddress=<verified-sender>
EmailSettings__DisplayName=StarterTemplate
PasswordRecovery__Mode=EmailOtp
PasswordRecovery__ResetPath=/iam/reset-password
Messaging__Transport=RabbitMq
Messaging__RabbitMq__Host=<rabbitmq-host>
Messaging__RabbitMq__Username=<rabbitmq-user>
Messaging__RabbitMq__Password=<rabbitmq-password>
CacheSettings__Provider=Redis
CacheSettings__Redis__ConnectionString=<redis-connection>
DataProtection__UseExternalKeyStore=true
DataProtection__RedisKeyRingConnectionString=<redis-connection>
DataProtection__RedisKeyRingKey=DataProtection-Keys
```

For Heroku and generic hosts, do not use local filesystem storage for private profile images or Data Protection keys in production. Use a persistent external provider.

## Storage And Data Protection Decision

Current implemented profile-image providers:

- `Local`
- `Azurite`
- `AzureBlob`
- `S3` (DigitalOcean Spaces, MinIO, AWS S3)

Current implemented Data Protection key-ring stores:

- Local filesystem
- Azure Blob Storage
- Redis

Current implemented Data Protection key encryption:

- `None`
- `KeyVault`
- `Certificate`
- `Auto`

For a non-Azure production deployment, you should configure the `S3` provider for profile images and point it to an S3-compatible object storage service like DigitalOcean Spaces. `Local` storage is acceptable only for ephemeral deployment confidence demos.

## Certificates

Use platform-managed TLS certificates for public HTTPS whenever possible.

- DigitalOcean App Platform can manage certificates for custom domains.
- Heroku Automated Certificate Management can manage certificates for custom domains.
- Application-level certificates should only be used for specific cryptographic features such as Data Protection certificate encryption, not as the default public TLS path.

## Deployment Checklist

1. Confirm the target branch has passed backend/frontend/container CI.
2. Create the target host app(s).
3. Add host runtime environment variables.
4. Add GitHub Actions secrets and variables.
5. Run `.github/workflows/non-azure-deploy.yml` manually.
6. Verify `/health/live` and `/health/ready`.
7. Smoke-test login, MFA, profile image behavior, cache, messaging, and payments if enabled.
8. Review logs for startup warnings, missing config, or provider fallback.

## Keep Azure Intact

Do not remove Azure settings to make non-Azure deployment work. Keep provider selection isolated:

- Azure deployment remains in `.github/workflows/deploy-azure.yml`.
- Non-Azure deployment lives in `.github/workflows/non-azure-deploy.yml`.
- Local infrastructure remains in `ops/local/docker-compose.yml`.
- Runtime provider choices remain in app configuration.
