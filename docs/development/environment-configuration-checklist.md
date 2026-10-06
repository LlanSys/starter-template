# Environment Configuration Checklist

This checklist explains how StarterTemplate should be configured locally, when developing locally against Azure services, when deployed to Azure, and when deployed to supported non-Azure container hosts.

Configuration is stamp-scoped. Before adding or renaming cloud settings, confirm the selected SaaS model in `docs/architecture/saas-multitenancy-strategy.md`: local development, pooled production stamp, or isolated tenant stamp.

## Configuration Modes

StarterTemplate uses `ASPNETCORE_ENVIRONMENT` for application behavior and normal .NET configuration precedence for values.

- `Development`: local developer experience. Non-secret defaults live in `appsettings.Development.json`; developer-specific secrets live in user-secrets.
- `Production`: deployed runtime behavior. Secrets come from App Service settings, Key Vault references, Key Vault configuration provider, or managed identity.
- Azure-backed local development: still uses `Development`, but selected services point at Azure through user-secrets. This is not a separate environment name; it is a configuration choice.

Avoid creating a custom environment name such as `Native` unless we have a clear hosting reason. It is simpler and safer to keep `Development` plus explicit Azure-backed settings.

## Where Values Belong

- `appsettings.json`: safe defaults, placeholders, feature structure, and non-secret values.
- `appsettings.Development.json`: safe local defaults and local-only toggles.
- User-secrets: local developer secrets such as SQL connection strings, JWT secret, Key Vault URI, provider API keys, and Azure storage connection strings.
- Environment variables: CI/CD, containers, App Service settings, and temporary overrides.
- Azure Key Vault: production secrets and managed secrets used by deployed apps.
- Azure App Service settings: runtime settings, Key Vault references, and non-secret deployment configuration.
- Non-Azure host environment/config variables: production runtime settings for DigitalOcean, Heroku, and generic Docker hosts.

In Azure Container Apps and App Service, the host receives stamp-specific environment variables and Key Vault references. The same container image should run in different stamps without code changes.

Never commit real passwords, account keys, connection strings, API keys, or temporary provisioning passwords.

`DataProtection:ApplicationName` is a cryptographic compatibility identifier, not a display label. Rename the template before its first run, then keep this value stable after protected cookies, TOTP secrets, or tokens have been issued. A later change requires an explicit key/ciphertext migration or security reset plan.

## Naming Rules

- .NET configuration sections use `:` locally, for example `DataProtection:BlobKeyUri`.
- App Service environment variables use `__`, for example `DataProtection__BlobKeyUri`.
- Key Vault secret names use `--`, for example `DataProtection--BlobKeyUri`.

When a Key Vault belongs to an isolated stamp, keep secret names simple and aligned with the POCO section path. Use product or tenant prefixes only for genuinely shared vaults or control-plane secrets.

## Azure Services Used By The Template

| Concern | Azure service | Primary configuration keys | Status |
| --- | --- | --- | --- |
| SQL database | Azure SQL or SQL Server | `ConnectionStrings:DefaultConnection` | Required for API runtime |
| Data Protection key ring | Azure Blob Storage | `DataProtection:BlobKeyUri` | Implemented |
| Data Protection key encryption | Azure Key Vault key | `DataProtection:KeyEncryptionMode`, `DataProtection:KeyVaultKeyIdentifier` | Implemented |
| Profile images | Azure Blob Storage | `ProfileImageStorage:Provider`, `ProfileImageStorage:AzureBlob:ContainerUri` | Implemented |
| UI-to-API calls | Blazor `HttpClient` | `BackendApi:BaseUrl`, `BackendApi:TransientRetryCount`, `BackendApi:TransientRetryDelayMilliseconds` | Implemented |
| Secret loading | Azure Key Vault | `KeyVault:Uri` | Implemented |
| App hosting | Azure App Service | App settings and managed identity | Ready for deployment work |
| Observability | Application Insights / Azure Monitor | `Observability:AzureMonitor:ConnectionString`, `ApplicationInsights:ConnectionString`, or `APPLICATIONINSIGHTS_CONNECTION_STRING` | Implemented with fallbacks |
| Messaging | RabbitMQ locally; Azure Service Bus in Azure | `Messaging:Transport`, provider-specific settings | Implemented; local Compose smoke remains |
| Caching | Redis locally; Azure Managed Redis in Azure | `CacheSettings:Provider`, provider-specific settings | Implemented with memory fallback and managed-identity support |
| Email delivery | Mailpit locally; provider API in Azure | `EmailSettings:Provider`, provider-specific settings | Implemented; SMTP credentials are not a production standard |
| HTTP output caching | ASP.NET Core OutputCache | `AddOutputCache`, `UseOutputCache`, endpoint policies | Implemented |
| Response compression | ASP.NET Core ResponseCompression | `ResponseCompression:Enabled`, `ResponseCompression:EnableForHttps` | Implemented |
| Feature flags | Configuration-backed feature gate | `FeatureFlags:Provider`, `FeatureFlags:Flags` | Implemented; fail-closed by default |
| SignalR | Authenticated notification hub | `/hubs/notifications` | Implemented baseline |
| Reporting | QuestPDF via reporting abstraction | `Reporting:QuestPdf:License` | Implemented baseline |
| Payments | Payment gateway abstraction with NoOp, Stripe, and M-Pesa adapters | `Payments:Provider`, `Payments:Stripe:*`, `Payments:Mpesa:*` | Code-wired; provider credentials/callback smoke tests are environment-specific certification |
| Entra ID SSO | Corporate OIDC auth scheme | `EntraId:Enabled`, `EntraId:TenantId`, `EntraId:ClientId`, `EntraId:ClientSecret` | OIDC scheme configurable; local AppUser linking and token issuance flow remains the dedicated IAM slice |
| Passkeys/WebAuthn | Browser passkey authentication | TBD | Pending dedicated IAM slice |
| Password recovery | Email OTP or email reset link | `PasswordRecovery:Mode`, `PasswordRecovery:ResetPath`, `EmailSettings:ClientBaseUrl` | Implemented; exactly one mode is active per deployment |

## Recommended Local Setup

Use user-secrets for local development:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<local-sql-connection>" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "JwtSettings:Secret" "<strong-local-jwt-secret>" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "IamProvisioning:TemporaryPassword" "<temporary-password>" --project src\Backend\Api\ST.Api\ST.Api.csproj
```

Password recovery is selected once per deployment. Use `EmailOtp` to send and verify a six-digit code before accepting a new password, or `EmailLink` to send an ASP.NET Identity reset-token link. Do not expose both flows simultaneously.

```powershell
dotnet user-secrets set "PasswordRecovery:Mode" "EmailOtp" --project src\Backend\Api\ST.Api\ST.Api.csproj

# Alternative deployment mode:
dotnet user-secrets set "PasswordRecovery:Mode" "EmailLink" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "EmailSettings:ClientBaseUrl" "https://localhost:7149" --project src\Backend\Api\ST.Api\ST.Api.csproj
```

Local email should normally be captured by Mailpit through the setup script:

```powershell
dotnet user-secrets set "EmailSettings:Provider" "LocalMailpit" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "EmailSettings:LocalMailpit:Host" "localhost" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "EmailSettings:LocalMailpit:Port" "1025" --project src\Backend\Api\ST.Api\ST.Api.csproj
```

Do not use personal mailbox SMTP credentials as the production email path. Production email should use an approved provider API, currently represented by `EmailSettings:Provider=SendGrid`.
SendGrid is Twilio SendGrid. It is managed from the Twilio/SendGrid platform, but it is separate from Twilio SMS or WhatsApp credentials.
The local setup script removes legacy SMTP-style secrets such as `EmailSettings:Username`, `EmailSettings:Password`, and `SmtpSettings:Password` so old personal-mailbox settings cannot accidentally confuse local testing.

Populate the complete local payment configuration in API user-secrets without overwriting existing values:

```powershell
./scripts/initialize-payment-settings.ps1
```

Local platform setup also calls this helper. Credential fields remain blank until replaced with provider-issued values. `Payments:Provider` defaults to `NoOp`; existing selections and credentials are preserved. The UI can select either configured provider per request. Stripe return URLs point to the existing `/features/payments` page; the History and Status Lookup tabs show payment outcomes.

`Payments:Mpesa:Environment` selects `Sandbox` or `Live`. `Payments:Mpesa:CallbackUrlBase` is the publicly reachable API origin, without a route. The adapter appends these implemented callback routes:

- Stripe: `/api/v1/shared/payments/stripe/webhook` (register this complete URL for the Stripe webhook and store its signing secret).
- M-Pesa STK: `/api/v1/shared/payments/mobile-money/stk-callback`.
- M-Pesa C2B validation: `/api/v1/shared/payments/mobile-money/c2b-validation`.
- M-Pesa C2B confirmation: `/api/v1/shared/payments/mobile-money/c2b-confirmation`.

Provider callbacks must reach the API host, not the Blazor host. Stripe also supports local webhook forwarding through its CLI; use the signing secret emitted by that listener for local verification ([Stripe webhook documentation](https://docs.stripe.com/webhooks)). M-Pesa needs a public API origin for its callbacks. No payment is submitted by the initialization helper. Restart the API from Visual Studio after editing provider configuration.

Payment providers are selected explicitly:

```powershell
dotnet user-secrets set "Payments:Provider" "NoOp" --project src\Backend\Api\ST.Api\ST.Api.csproj

# Stripe
dotnet user-secrets set "Payments:Provider" "Stripe" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "Payments:Stripe:SecretKey" "<stripe-secret-key>" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "Payments:Stripe:WebhookSigningSecret" "<stripe-webhook-signing-secret>" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "Payments:Stripe:SuccessUrl" "https://localhost:7149/features/payments" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "Payments:Stripe:CancelUrl" "https://localhost:7149/features/payments" --project src\Backend\Api\ST.Api\ST.Api.csproj

# M-Pesa Daraja
dotnet user-secrets set "Payments:Provider" "Mpesa" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "Payments:Mpesa:ConsumerKey" "<daraja-consumer-key>" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "Payments:Mpesa:ConsumerSecret" "<daraja-consumer-secret>" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "Payments:Mpesa:ShortCode" "<paybill-or-till>" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "Payments:Mpesa:PassKey" "<daraja-passkey>" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "Payments:Mpesa:AuthEndpoint" "https://sandbox.safaricom.co.ke/oauth/v1/generate?grant_type=client_credentials" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "Payments:Mpesa:StkPushEndpoint" "https://sandbox.safaricom.co.ke/mpesa/stkpush/v1/processrequest" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "Payments:Mpesa:StkQueryEndpoint" "https://sandbox.safaricom.co.ke/mpesa/stkpushquery/v1/query" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "Payments:Mpesa:CallbackUrlBase" "https://<public-api-host>" --project src\Backend\Api\ST.Api\ST.Api.csproj
```

Entra ID SSO is off by default. Enable it only after registering the application in Microsoft Entra ID and adding the redirect URI matching `EntraId:CallbackPath`:

```powershell
dotnet user-secrets set "EntraId:Enabled" "true" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "EntraId:TenantId" "<tenant-id>" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "EntraId:ClientId" "<app-client-id>" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "EntraId:ClientSecret" "<app-client-secret>" --project src\Backend\Api\ST.Api\ST.Api.csproj
```

## Provider Readiness Without Enabling Providers

StarterTemplate keeps provider integrations code-ready and configuration-ready without forcing every cloned application to use every provider.

The standard is:

- Keep typed JSON sections and settings POCOs present for every supported provider.
- Keep local defaults safe: `Payments:Provider=NoOp`, `EntraId:Enabled=false`, `EmailSettings:Provider=LocalMailpit`, and `Messaging:Transport=RabbitMq`.
- Add real provider secrets only when that provider is selected for a local smoke test or deployed environment.
- Fail fast when a provider is selected but required values are missing.

Do not store placeholder API keys in user-secrets. Empty placeholders belong in JSON; user-secrets should contain real local developer values only.

Provider-specific values to collect when enabling each integration:

| Provider | Enable when | Required local user-secrets |
| --- | --- | --- |
| Twilio SendGrid email | Testing production-style email delivery | `EmailSettings:Provider=SendGrid`, `EmailSettings:SendGrid:ApiKey`, `EmailSettings:FromAddress`, `EmailSettings:ClientBaseUrl` |
| Stripe payments | Testing card/checkout flow | `Payments:Provider=Stripe` or per-request provider selection, `Payments:Stripe:SecretKey`, `Payments:Stripe:WebhookSigningSecret`, `Payments:Stripe:SuccessUrl`, `Payments:Stripe:CancelUrl` |
| M-Pesa Daraja | Testing STK Push/query flow | `Payments:Provider=Mpesa` or per-request provider selection, `Payments:Mpesa:ConsumerKey`, `Payments:Mpesa:ConsumerSecret`, `Payments:Mpesa:ShortCode`, `Payments:Mpesa:PassKey`, endpoints, and public callback URL |
| Entra ID SSO | Testing corporate OIDC login | `EntraId:Enabled=true`, `EntraId:TenantId`, `EntraId:ClientId`, `EntraId:ClientSecret` |
| Azure Service Bus | Testing Azure messaging transport | `Messaging:Transport=AzureServiceBus`, `Messaging:AzureServiceBus:ConnectionString` |

If Azure subscription access is unavailable, leave Entra ID and Azure Service Bus disabled. The JSON sections, settings POCOs, DI registration, and fail-fast validation remain ready for the future subscription upgrade.

For local-only development, leave these values empty so the app uses filesystem/local storage:

```powershell
dotnet user-secrets remove "DataProtection:BlobKeyUri" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets remove "DataProtection:KeyVaultKeyIdentifier" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "ProfileImageStorage:Provider" "Local" --project src\Backend\Api\ST.Api\ST.Api.csproj
# For local Redis, prefer the setup script because it generates matching ignored credentials.
./scripts/setup-local-platform.ps1
```

Azure migrations/deployments are paused unless the repository variable AZURE_DEPLOYMENT_ENABLED is exactly true. Builds/tests and GHCR publishing continue while paused. Once subscription access and the production checklist are ready, enable the flag and dispatch Deploy to Azure with app-service, aca-acr, or aca-ghcr; main merges also deploy when enabled.

## Recommended Azure-Backed Local Setup

Use this when testing Azure integrations from your machine. Your Visual Studio or Azure CLI identity must have the required Azure RBAC roles.

```powershell
dotnet user-secrets set "DataProtection:KeyEncryptionMode" "KeyVault" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "DataProtection:BlobKeyUri" "https://<storage-account>.blob.core.windows.net/dataprotection-keys/keyring.xml" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "DataProtection:KeyVaultKeyIdentifier" "https://<vault>.vault.azure.net/keys/<key>/<version>" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "ProfileImageStorage:Provider" "AzureBlob" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "ProfileImageStorage:AzureBlob:ContainerUri" "https://<storage-account>.blob.core.windows.net/profile-images" --project src\Backend\Api\ST.Api\ST.Api.csproj
```

If your local Azure identity is not ready, use `ProfileImageStorage:AzureBlob:ConnectionString` and `ProfileImageStorage:AzureBlob:ContainerName` only as a controlled local fallback.

## Recommended Azure Production Setup

In Azure App Service:

```text
ASPNETCORE_ENVIRONMENT=Production
KeyVault__Uri=https://<vault>.vault.azure.net/
DataProtection__ApplicationName=StarterTemplate
DataProtection__KeyEncryptionMode=KeyVault
DataProtection__BlobKeyUri=https://<storage-account>.blob.core.windows.net/dataprotection-keys/keyring.xml
DataProtection__KeyVaultKeyIdentifier=https://<vault>.vault.azure.net/keys/<key>/<version>
ProfileImageStorage__Provider=AzureBlob
ProfileImageStorage__AzureBlob__ContainerUri=https://<storage-account>.blob.core.windows.net/profile-images
ProfileImageStorage__AzureBlob__BlobPrefix=profile-images
EmailSettings__Provider=SendGrid
EmailSettings__SendGrid__Endpoint=https://api.sendgrid.com/v3/mail/send
EmailSettings__SendGrid__ApiKey=<key-vault-reference-or-app-setting>
EmailSettings__FromAddress=noreply@your-domain.example
EmailSettings__DisplayName=StarterTemplate
PasswordRecovery__Mode=EmailOtp
PasswordRecovery__ResetPath=/iam/reset-password

# Payments
Payments__Provider=NoOp|Stripe|Mpesa
Payments__Stripe__SecretKey=<key-vault-reference>
Payments__Stripe__WebhookSigningSecret=<key-vault-reference>
Payments__Stripe__SuccessUrl=https://your-app/features/payments
Payments__Stripe__CancelUrl=https://your-app/features/payments

Payments__Mpesa__ConsumerKey=<key-vault-reference>
Payments__Mpesa__ConsumerSecret=<key-vault-reference>
Payments__Mpesa__ShortCode=<paybill-or-till>
Payments__Mpesa__PassKey=<key-vault-reference>
Payments__Mpesa__AuthEndpoint=https://api.safaricom.co.ke/oauth/v1/generate?grant_type=client_credentials
Payments__Mpesa__StkPushEndpoint=https://api.safaricom.co.ke/mpesa/stkpush/v1/processrequest
Payments__Mpesa__StkQueryEndpoint=https://api.safaricom.co.ke/mpesa/stkpushquery/v1/query
Payments__Mpesa__CallbackUrlBase=https://your-api

# Entra ID SSO
EntraId__Enabled=false
EntraId__TenantId=<tenant-id>
EntraId__ClientId=<client-id>
EntraId__ClientSecret=<key-vault-reference>

# Messaging
Messaging__Transport=AzureServiceBus
Messaging__AzureServiceBus__ConnectionString=<key-vault-reference-or-app-setting>

# Redis Cache - Option 1: Access Keys (Standard)
CacheSettings__Provider=AzureManagedRedis
CacheSettings__Azure__ConnectionString=your-redis-name.redis.cache.windows.net:6380,password=PRIMARY_ACCESS_KEY,ssl=True,abortConnect=False
CacheSettings__Azure__UseEntraId=false

# Redis Cache - Option 2: Microsoft Entra ID (Passwordless / Managed Identity)
CacheSettings__Provider=AzureManagedRedis
CacheSettings__Azure__ConnectionString=your-redis-name.redis.cache.windows.net:6380,ssl=True,abortConnect=False
CacheSettings__Azure__UseEntraId=true
CacheSettings__Azure__PrincipalId=<User-Assigned-Identity-Client-ID-Or-Leave-Blank-For-System-Assigned>

Optional observability keys:

```text
APPLICATIONINSIGHTS_CONNECTION_STRING=<application-insights-connection-string>
```

or Key Vault:

```text
ApplicationInsights--ConnectionString=<application-insights-connection-string>
```

## Recommended Non-Azure Production Setup

Use this when deploying through `.github/workflows/non-azure-deploy.yml`.

Minimum API settings:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<sql-server-connection>
JwtSettings__Secret=<strong-secret>
KeyVault__Uri=
DataProtection__ApplicationName=StarterTemplate
DataProtection__UseExternalKeyStore=true
DataProtection__RedisKeyRingConnectionString=<redis-connection>
DataProtection__RedisKeyRingKey=DataProtection-Keys
EmailSettings__Provider=SendGrid
EmailSettings__SendGrid__Endpoint=https://api.sendgrid.com/v3/mail/send
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
```

Minimum Blazor settings:

```text
ASPNETCORE_ENVIRONMENT=Production
BackendApi__BaseUrl=https://<api-host>/
```

DigitalOcean and Heroku both expose environment variables/config vars. These are the non-Azure equivalent of App Service app settings, not a full Key Vault replacement. For stronger secret governance, use an external vault or the platform's managed secret integration when available.

For Heroku, the application reads the platform-assigned `PORT` variable automatically. Do not hardcode `ASPNETCORE_URLS` to a fixed port unless the host supports that port.

For non-Azure production profile images and Data Protection keys, avoid ephemeral local filesystems. Use Redis for Data Protection key-ring persistence, and use an implemented durable provider or add the required provider before certification:

- S3-compatible object storage for profile images.
- Durable private volume for profile images only when the host documents persistence, backup, and restore ownership.

See [Non-Azure Deployment Configuration](non-azure-deployment.md).

## Azure RBAC Checklist

Grant these roles before testing Azure-backed local development or App Service deployment:

- Developer user or App Service managed identity: `Storage Blob Data Contributor` on the storage account or required containers.
- Developer user or App Service managed identity: `Key Vault Crypto User` on the Data Protection key.
- Developer user who creates keys: `Key Vault Crypto Officer` or `Key Vault Administrator`.
- App Service: system-assigned managed identity enabled.
- Developer user or App Service managed identity (when UseEntraId=true): `Redis Data Contributor` (or `Redis Data Owner`) role on the Azure Cache for Redis instance.

## Current Configuration Review Rules

When checking screenshots or app settings, verify:

- Mode/provider/transport settings use supported values exactly: `DataProtection__KeyEncryptionMode` = `Auto`, `KeyVault`, `Certificate`, or `None`; `ProfileImageStorage__Provider` = `Local` or `AzureBlob`; `Messaging__Transport` = `RabbitMq` or `AzureServiceBus`; `CacheSettings__Provider` = `Auto`, `Memory`, `Redis`, or `AzureManagedRedis`.
- `DataProtection__BlobKeyUri` points to the `dataprotection-keys` container and a blob name such as `keyring.xml`.
- `DataProtection__RedisKeyRingConnectionString` is set for non-Azure production hosts that use Redis for Data Protection key persistence.
- `DataProtection__ApplicationName` is identical across every instance and deployment slot and remains unchanged after protected data is issued.
- `DataProtection__KeyEncryptionMode` is `KeyVault` or `Auto` for Azure.
- `DataProtection__KeyVaultKeyIdentifier` points to a Key Vault key version, not a secret or certificate.
- `DataProtection__CertificateThumbprint` is empty unless deliberately using certificate mode.
- `ProfileImageStorage__Provider` is `AzureBlob` when testing Azure profile image uploads.
- `ProfileImageStorage__Provider` is not `Local` for non-Azure production unless the host provides a durable private volume and backup plan.
- `ConnectionStrings--DefaultConnection` exists in Key Vault or `ConnectionStrings__DefaultConnection` exists in App Service.
- Cache settings: If `CacheSettings:Azure:UseEntraId` is `true`, ensure the API connection string does not contain a password and has RESP3 enabled. For a user-assigned managed identity, `CacheSettings:Azure:PrincipalId` must contain its client ID.
- `JwtSettings--Secret` or `JwtSettings--SecretKey` exists in Key Vault.
- `EmailSettings:Provider` is `LocalMailpit` only in Development, `SendGrid` in Production, and `EmailSettings:FromAddress` is a verified sender/domain with the selected provider.
- Use `OutputCache` for API response caching. Do not add legacy `ResponseCaching` unless a future requirement specifically needs HTTP header/proxy cache semantics.
- `Microsoft.OpenApi` is pinned directly to a non-vulnerable compatible 2.x version because the ASP.NET OpenAPI source generator is not yet compatible with the 3.x package line.

## CI/CD Pipeline and Deployments

StarterTemplate deploy-azure.yml runs on main pushes and manual dispatch. AZURE_DEPLOYMENT_ENABLED=true permits migration execution, App Service/Container Apps deployment and ACR publishing. False or missing skips these jobs before Azure sign-in, firewall writes or database connections. Builds/tests, artifact and migration-bundle generation and GHCR publishing remain available. Manual dispatch respects the same flag.

### Deployment Prerequisites

1. Configure the `azure-prod` GitHub environment and Microsoft Entra OIDC federation.
2. Create the GitHub deployment identity as an Azure SQL contained user with migration permissions.
3. Add the required Azure resource-name variables and passwordless SQL connection-string secret.
4. Keep the App Service managed identities configured separately for runtime Azure access.

Follow [Azure CI/CD Configuration](azure-cicd-configuration.md) for the complete portal, GitHub, SQL, RBAC, networking, and troubleshooting steps.

## Troubleshooting

- If sign-in shows “The identity service is unavailable,” first confirm the API is running and the UI `BackendApi:BaseUrl` points at the API URL.
- If the first sign-in click fails after starting from Visual Studio but the second succeeds, check whether the API was still warming up. The UI retries transport-level API failures using `BackendApi:TransientRetryCount` and `BackendApi:TransientRetryDelayMilliseconds`; backend logs should still be reviewed if retries are frequent.
- If sign-in fails with a Data Protection `403`, the identity used by the app lacks `Storage Blob Data Contributor` for the key-ring blob/container.
- If Data Protection fails with Key Vault authorization errors, the identity lacks `Key Vault Crypto User` for the key.
- If profile upload succeeds visually but no blob appears, confirm the backend response succeeded, `ProfileImageStorage:Provider=AzureBlob`, and the current user has completed required MFA enrollment.
- If old package errors mention `FluentEmail.MailKit`, clean stale build output and rebuild; source code should no longer reference FluentEmail.
- If migration bundles report `DefaultAzureCredential failed`, GitHub OIDC is missing or its federated subject does not match the `production` environment. App Service managed identity is not available to a GitHub-hosted runner.
