# Local Platform

StarterTemplate runs application code from Visual Studio and runs replaceable infrastructure dependencies in Docker. This preserves normal .NET debugging while making a new developer machine reproducible.

Docker Desktop groups these services under the Compose project name `cg-startertemplate`. The `cg-` prefix identifies the row as a container group rather than an individual service.

Data volumes retain stable `startertemplate_*` names independently of the group label. The setup script creates missing volumes and reuses existing ones, allowing container-group renames and container recreation without abandoning local data.

## Runtime Boundary

Run from Visual Studio:

- `ST.Api`
- `ST.UI.Blazor`
- [Local database checks and migrations](starter-setup.md#check-and-update-an-existing-local-database) and application debugging

Run in Docker:

- RabbitMQ for local MassTransit messaging
- Redis for distributed cache and HybridCache L2
- Mailpit for safe local email capture
- Seq when a locally installed Seq instance is not already available
- Azurite for local Azure Blob-compatible profile-image storage
- SQL Server when a developer does not want to use an installed SQL Server instance
- API and Blazor app containers when testing local deployability instead of Visual Studio debugging

Azure Service Bus is an Azure-managed service and does not run in Docker. The same MassTransit consumers use RabbitMQ locally and Azure Service Bus in Azure by changing `Messaging:Transport` and its provider settings.

## First-Time Setup

Docker Desktop must be running. From the repository root:

```powershell
./scripts/setup-local-platform.ps1
```

The script:

1. Generates random local-only RabbitMQ, Redis, SQL Server, and Azurite credentials in ignored `ops/local/.env`.
2. Writes matching RabbitMQ, Redis, and Mailpit provider values to API user-secrets, and adds missing payment fields without overwriting existing credentials.
3. Pulls and starts RabbitMQ, Redis, Mailpit, and Azurite.
4. Waits for container health checks.

Run the script from the repository root, the folder containing `StarterTemplate.sln`, `AGENTS.md`, and the `scripts` folder:

```powershell
cd path\to\StarterTemplate
./scripts/setup-local-platform.ps1
```

It is a repository script, not a globally installed PowerShell command. You may invoke it from another folder only by supplying its full path.

Optional profiles:

```powershell
# Add Seq when port 5344 is not already used by a local Seq installation.
./scripts/setup-local-platform.ps1 -IncludeSeq

# Add SQL Server on localhost,14335 and update the API connection-string secret.
./scripts/setup-local-platform.ps1 -IncludeSqlServer

# Start every local dependency.
./scripts/setup-local-platform.ps1 -IncludeSqlServer -IncludeSeq
```

Do not use `-IncludeSqlServer` when retaining an existing SQL Server/SSMS database connection. The script leaves the existing `ConnectionStrings:DefaultConnection` secret unchanged unless that switch is supplied.

The setup script is idempotent and can be run whenever Docker services or local user-secrets need to be aligned. It creates credentials only when the ignored `ops/local/.env` file does not exist; later runs reuse those credentials and safely reconcile containers. It is not required before every Visual Studio launch when the containers are already running and the secrets have not changed.

The standard Visual Studio setup selects `ProfileImageStorage:Provider=Azurite`, uses the separate `ProfileImageStorage:Azurite` section with `UseDevelopmentStorage=true`, and targets the private `profile-images` container. The containerized app profile uses the randomly generated `AZURITE_ACCOUNT_NAME` and `AZURITE_ACCOUNT_KEY` values from ignored `ops/local/.env`; no shared emulator key is committed. Filesystem storage remains available as an explicit fallback by selecting `Provider=Local`. The `ProfileImageStorage:AzureBlob` section remains independent and preserved for Azure production.

## Local App Deployment

Use this when you want the local machine to behave like a small deployment target: Docker builds the API and Blazor images, starts them with the same infrastructure dependencies, and injects configuration through environment variables instead of `dotnet user-secrets`.

From the repository root:

```powershell
./scripts/setup-local-app-deployment.ps1
```

By default, the app containers expect `APP_SQL_CONNECTION_STRING` in ignored `ops/local/.env`. This should point to the SQL Server that containers can reach. For SQL Server installed on the host machine, Docker Desktop exposes the host as `host.docker.internal`, for example:

```text
APP_SQL_CONNECTION_STRING=Server=host.docker.internal;Database=ST;User Id=sa;Password=<your-local-password>;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True
```

The SQL password in `APP_SQL_CONNECTION_STRING` is not the StarterTemplate sign-in password. The development admin user is seeded from `DevelopmentSeed:AdminEmail` and `DevelopmentSeed:AdminPassword`; both are blank by default and seeding is disabled. Configure DevelopmentSeed values in user-secrets before enabling it.

If you want a fully containerized stack including SQL Server, run:

```powershell
./scripts/setup-local-app-deployment.ps1 -UseContainerSql
```

The app deployment overlay:

- Builds `ST.Api` from `ops/docker/api.Dockerfile`.
- Builds `ST.UI.Blazor` from `ops/docker/blazor.Dockerfile`.
- Uses RabbitMQ, Redis, Mailpit, and Azurite through Docker service names.
- Disables development identity seeding for the app containers. Use an account already configured in the local database.
- Stores ASP.NET Core Data Protection keys in Redis using `DataProtection:KeyEncryptionMode=None` for local-only development.
- Keeps Azure Blob, Key Vault, Service Bus, and provider production settings untouched.

### Sharing the local API key ring

When Visual Studio and Docker use the same application database, configure both APIs to use the same Data Protection key ring and keep `DataProtection:ApplicationName` at `StarterTemplate`. The Windows API defaults to DPAPI-protected files in `%LOCALAPPDATA%\STApi\DataProtection-Keys`. Docker uses the `StarterTemplate-DataProtection-Keys` Redis key. When an authenticator's original key is unavailable, verification fails and the enrollment stays enabled.

For a fresh local database, opt in before enrolling users. Add `-UseSharedLocalKeyRing` to either setup script to configure the Visual Studio API to use the Docker Redis ring. The setting persists in the local API user-secrets. Restart the Windows API to pick up the new ring. Preserve the Redis data volume and the original Windows key directory. This uses unencrypted-at-rest keys in the local Redis service. Restrict access to that service. Production key encryption and Blazor's separate browser-storage key ring follow their own configuration.

#### Preserve an existing Windows enrollment

Check all databases served by the local API before changing its key ring. Tenant and deployment stamp database credentials and sensitive organization settings also use this ring. The command below handles one explicitly selected IAM account. Review other accounts and any other protected data separately before switching the API.

Run the tool as the Windows user who owns the original DPAPI keys. Start the local Docker API first so the destination ring has keys, then pause both API hosts while checking and applying the repair. Keep SQL Server or PostgreSQL and Redis running. Supply these variables in the current shell without printing their values.

| Variable | Value |
| --- | --- |
| `ST_DATABASE_CONNECTION` | Existing IAM database connection string with a loopback host and explicit database name |
| `ST_AUTHENTICATOR_USER_ID` | ID of the existing local IAM account |
| `ST_LOCAL_SOURCE_KEY_DIRECTORY` | Original Windows key directory |
| `ST_LOCAL_REDIS_CONNECTION` | Existing local Docker Redis connection string with a loopback host and the published port |

```powershell
dotnet run --project tools/ST.DatabaseTools -- SqlServer check-local-authenticator
dotnet run --project tools/ST.DatabaseTools -- SqlServer reprotect-local-authenticator
dotnet run --project tools/ST.DatabaseTools -- SqlServer check-local-authenticator
```

Use `PostgreSql` for a PostgreSQL database. The first check validates the conversion without saving. The repair changes ciphertext in one transaction and preserves the same authenticator enrollment. The second check should report zero records requiring reprotection. After checking any other protected records, use `-UseSharedLocalKeyRing`, restart both APIs, and verify a fresh authenticator code through each host. If the original keys are unavailable, use the approved account recovery process.

Local Identity lockout is enabled to match production behavior. Five failed password attempts lock the account for five minutes. During local testing, wait for the lockout window to expire or reset the local user row intentionally in the development database; do not keep retrying because each failed password attempt increments the Identity lockout counter.

Default app endpoints:

| App | Endpoint |
| --- | --- |
| API | `http://localhost:8180` |
| Scalar | `http://localhost:8180/scalar/v1` |
| Blazor UI | `http://localhost:8181` |

For Docker sign-in, configure the `APP_GOOGLE_SSO_*` and `APP_ENTRA_SSO_*` values in the ignored `ops/local/.env` and enable the providers you configured. The default API callbacks are `http://localhost:8180/signin-google` and `http://localhost:8180/signin-oidc`. Rerun the app deployment script to apply changes.

Apply the Shared `Quartz46SchemaCompatibility` migration before starting the scheduler on an older database. It adds the required Quartz 4.6.2 tables and columns for SQL Server and PostgreSQL and keeps existing jobs, triggers and JSON job data. Rolling back the migration history retains the added schema to protect scheduler data. The Docker API enables background jobs by default. `APP_BACKGROUND_JOBS_ENABLED=false` remains available for local troubleshooting.

The full app deployment is for deployability confidence and local smoke tests. For day-to-day breakpoint debugging, continue running API and Blazor from Visual Studio while keeping the infrastructure containers running.

## Local Endpoints

| Service | Endpoint | Purpose |
| --- | --- | --- |
| RabbitMQ | `localhost:5675` | AMQP transport |
| RabbitMQ Management | `http://localhost:15675` | Exchanges, queues, consumers, and message rates |
| Redis | `localhost:6380` | HybridCache distributed layer; avoids common existing port `6379` |
| Mailpit | `http://localhost:8030` | Captured email UI |
| Mailpit SMTP | `localhost:1025` | Local-only capture relay used by `EmailSettings:Provider=LocalMailpit` |
| Seq | `http://localhost:5344` | Structured logs, optional profile |
| Azurite Blob | `http://localhost:10020` | Storage emulator, optional profile |
| SQL Server | `localhost,14335` | Containerized database, optional profile |
| Containerized API | `http://localhost:8180` | Optional local deployment profile |
| Containerized Blazor | `http://localhost:8181` | Optional local deployment profile |

## Operations

```powershell
# Inspect service state.
docker compose --env-file ops/local/.env -f ops/local/docker-compose.yml ps

# Stop containers while preserving data volumes.
./scripts/stop-local-platform.ps1

# Include the same optional profiles that were started.
./scripts/stop-local-platform.ps1 -IncludeSeq

# Restart the core platform.
docker compose --env-file ops/local/.env -f ops/local/docker-compose.yml up -d --wait

# Restart the deployable app profile.
docker compose --env-file ops/local/.env -f ops/local/docker-compose.yml -f ops/local/docker-compose.apps.yml --profile apps up -d --wait
```

The repository stop script deliberately exposes no volume-deletion option. It cannot prevent a developer from invoking Docker directly, so do not run `docker compose down -v` unless local RabbitMQ, Redis, SQL, Seq, and Azurite data should be permanently deleted.

## RabbitMQ Outbox Certification

Run the real RabbitMQ round-trip test from the repository root:

```powershell
./scripts/test-local-messaging.ps1
```

The script reads generated RabbitMQ credentials from the ignored local environment file without printing them, ensures RabbitMQ is healthy, and runs the `ExternalRabbitMq` integration test. The test:

1. Starts MassTransit against the Docker RabbitMQ broker.
2. Publishes a uniquely identified event through the EF Core bus outbox.
3. Commits the SQLite transaction containing the outbox record.
4. Waits for MassTransit's outbox delivery service to publish the event.
5. Verifies a real RabbitMQ consumer receives the same event.
6. Verifies the EF outbox record is drained.

The ordinary integration suite skips this external test when Docker credentials are unavailable. This keeps CI deterministic while the dedicated script certifies the complete local transport path.

## Azure Deployment Dispatch

The Azure deployment workflow remains committed but is manual-only. Normal backend, frontend, mobile, full-solution, and architecture CI remain independent of Azure deployment.

1. Open **Actions > Deploy to Azure > Run workflow**.
2. Select `app-service`, `aca-acr`, or `aca-ghcr` for `deployment_target`.
3. Confirm the selected target's OIDC variables, SQL connection secret, managed identities, and production environment approval are ready before dispatching.

Pushes and pull-request merges must not create migration bundles, run Azure migrations, or deploy resources.

## Provider Mapping

| Concern | Local | Azure production |
| --- | --- | --- |
| Messaging | RabbitMQ | Azure Service Bus |
| Distributed cache | Redis container | Azure Managed Redis using managed identity |
| SQL | Installed SQL Server or SQL container | Azure SQL |
| Email | Mailpit | Approved email API provider, currently `SendGrid` |
| Logs | Seq | Azure Monitor/Application Insights, optionally Seq |
| Profile images | Local filesystem or Azurite | Private Azure Blob Storage |
| Data Protection | Local key ring | Azure Blob key ring encrypted by Key Vault |

The Azure OIDC workflow, migration bundles, App Service deployment jobs, managed-identity configuration, and Key Vault/Blob settings remain in the repository. Local configuration overrides them through `appsettings.Development.json` and user-secrets; it does not replace them.

Development defaults to filesystem Data Protection and selects the local Mailpit email provider. The shared local API ring is an explicit opt-in. Existing Azure Blob/Key Vault URIs and production email provider secrets can remain configured without being contacted by the local runtime.

## Messaging License Decision

StarterTemplate pins MassTransit `8.5.12`, which targets .NET 10 and is published under Apache-2.0. MassTransit v9 is a commercial release and is not used by the reusable template. Revisit this decision deliberately if a future application purchases MassTransit v9 support and features.

Seq first-run administrator credentials use `SEQ_PASSWORD` from the ignored local `.env`. The setup script generates this value when missing. Changing it does not reset credentials in an existing Seq data volume.

## Concurrent applications

The setup script runs scripts/check-local-endpoints.ps1 before Compose startup. The checker resolves all profiles of discovered Compose groups and checks host port allocations, launch profiles, user-secret IDs and stale container bindings. Supply -ComposeFiles for new groups with no containers yet. Resolve reported conflicts before startup; native services still require a separate listener check. See [workstation endpoint allocations](workstation-endpoints.md).

Azurite user-secrets are generated from the account name and key in the ignored local .env; the generic UseDevelopmentStorage shortcut does not match a custom account. Each application has independent user-secret IDs and must provision its own values.
