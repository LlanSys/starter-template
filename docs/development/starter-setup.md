# Set Up A StarterTemplate Clone

StarterTemplate contains its own application code, plans, scripts and tests. A downstream application uses the StarterTemplate revision it clones; no separate repository, source commit or extraction document is required to rename, configure, build or extend it.

The retained contexts are IAM, Shared and ControlPlane. Each has a fresh InitialStarter migration for SQL Server and PostgreSQL. Business entities and policies belong in the application clone. See the certification evidence and remaining environment-specific checks in the refactor plan.

Use the [downstream configuration and release checklist](downstream-configuration-checklist.md) for every new application and environment. It covers settings the rename script does not allocate and records acceptance evidence.

## Rename before first run

Clone StarterTemplate into the new application's directory and run its rename script from that root:

```powershell
.\scripts\rename-template.ps1 -NewName MyApplication -NamespacePrefix MA -WhatIf
.\scripts\rename-template.ps1 -NewName MyApplication -NamespacePrefix MA
```

Review the diff and set the clone's Git remote to its own repository. The script updates product/project/namespace names, Dockerfiles, and assigns fresh host UserSecretsId values. It does not copy local secret contents or configure live cloud resources. Allocate distinct host ports and Compose project/volume names before starting applications side by side.

Keep DataProtection:ApplicationName stable after the first protected cookie, token or TOTP secret is issued. See [Renaming the template](renaming-the-template.md).

## Configure the local application

Development seeding is disabled, and account passwords are blank. Configure the new API project's user-secrets with these keys before enabling bootstrap:

- ConnectionStrings:DefaultConnection: the clone's own database connection.
- JwtSettings:SecretKey: a strong random signing secret.
- DevelopmentSeed:AdminUserName, AdminEmail and AdminPassword: the clone's administrator credentials.
- DevelopmentSeed:TenantId and Tenant:DefaultTenantId: the same locally configured tenant identifier.
- DevelopmentSeed:Enabled: true only when deliberately bootstrapping that local database.

Bootstrap creates one independent system administrator, seeds only the platform administrator role, and preserves passwords/lifecycle on normal reruns. It rejects tenant/identity conflicts and inactive or deleted accounts, and never deletes unrelated users. ResetExistingAdminPassword is explicit development configuration.

Run scripts/setup-local-platform.ps1 for RabbitMQ, Redis, Mailpit and Azurite. Include SQL Server/Seq only when needed; see [Local platform](local-platform.md). Run scripts/check-local-endpoints.ps1 with the new Compose files before infrastructure startup. Run application hosts from Visual Studio.

| Service | StarterTemplate defaults before clone-specific allocation |
| --- | --- |
| API HTTP / HTTPS | 5257 / 7229 |
| Blazor HTTP / HTTPS | 5206 / 7149 |
| Container API / Blazor | 8180 / 8181 |
| SQL Server | 14335; database ST |
| Redis | 6382 |
| RabbitMQ / management | 5675 / 15675 |
| Mailpit SMTP / UI | 1030 / 8030 |
| Seq | 5344 |
| Azurite blob / queue / table | 10020 / 10021 / 10022 |

Container service ports remain unchanged. Compose project cg-startertemplate and startertemplate_* volumes must be renamed for each clone. Development email uses LocalMailpit; deployed email uses a configured provider API.

## Restore and verify

After renaming, run restore on the renamed solution, then build its API and Web projects and run unit/architecture tests using the new project names. The current StarterTemplate names are:

```powershell
dotnet restore StarterTemplate.slnx
dotnet build src/Backend/Api/ST.Api/ST.Api.csproj --no-restore -p:UseSharedCompilation=false
dotnet build src/Frontend/Web/ST.UI.Blazor/ST.UI.Blazor.csproj --no-restore -p:UseSharedCompilation=false
dotnet test tests/ST.Tests.Unit/ST.Tests.Unit.csproj --no-restore -p:UseSharedCompilation=false
dotnet test tests/ST.Tests.Architecture/ST.Tests.Architecture.csproj --no-restore -p:UseSharedCompilation=false
```

Use fresh databases only. These baselines replace the template history and are not upgrade migrations for existing applications. Run provider integration tests and browser/email flows against the clone's own configuration. Optional SSO providers and physical passkey authenticators require their own configured smoke tests.

## Deployment configuration

Each downstream repository owns its GitHub secrets/variables, environment bindings, OIDC identities and cloud targets. Azure deployment/provisioning require AZURE_DEPLOYMENT_ENABLED=true in that repository. Keep the switch false while configuring and validating new targets. Choose application-specific resource prefixes, database names and deployment environment names; regenerate compiled Bicep JSON after infrastructure changes.

StarterTemplate currently uses starter-azure-prod/starter-non-azure-prod environments and stapp/stprod resource defaults. These are templates to customize, not preconfigured cloud resources.

## Apply the fresh database baseline

Set DatabaseSettings:Provider to SqlServer or PostgreSql and configure matching ControlPlaneConnection, IamConnection, SharedConnection and DefaultConnection strings. DefaultConnection is also used by Quartz. Keep these connections on the same local database unless deliberately configuring deployment stamps.

Restore the pinned tools with `dotnet tool restore`, then apply ControlPlane, IAM and Shared in that order. For SQL Server:

```powershell
dotnet ef database update --project src/Backend/Persistence/ST.Persistence/ST.Persistence.csproj --startup-project src/Backend/Api/ST.Api/ST.Api.csproj --context ControlPlaneSqlServerDBContext
dotnet ef database update --project src/Backend/Persistence/ST.Persistence/ST.Persistence.csproj --startup-project src/Backend/Api/ST.Api/ST.Api.csproj --context IamSqlServerDBContext
dotnet ef database update --project src/Backend/Persistence/ST.Persistence/ST.Persistence.csproj --startup-project src/Backend/Api/ST.Api/ST.Api.csproj --context SharedSqlServerDBContext
```

For PostgreSQL substitute PostgreSql for SqlServer in all three context names. Each context owns its migration history table. Shared creates Quartz tables along with platform payment, notification and outbox persistence. Run application hosts from Visual Studio after migrations.

## Check and update an existing local database

From the StarterTemplate root, run the check after pulling changes or scaffolding migrations.

```powershell
.\scripts\update-local-database.ps1
.\scripts\update-local-database.ps1 -Apply
# Use -Provider PostgreSql for a PostgreSQL database.
```

The default command lists pending migration IDs for ControlPlane, IAM and Shared. With `-Apply`, all three contexts are checked before any migration runs. A missing StarterTemplate baseline, unknown migration, history gap, or model drift blocks the update. The command rechecks all three histories after applying migrations. Each context is upgraded separately. If a later context fails, correct the reported problem and rerun the check before applying again.

The command uses the API's Development configuration, user-secrets and process environment. SQL Server is the default. The provider must match `DatabaseSettings:Provider`, and the three contexts must share `ConnectionStrings:DefaultConnection`. Set `ST_DATABASE_CONNECTION` in the current process to select an explicit local application database instead. Both paths reject remote and system databases. Keep database credentials out of arguments and logs.

Keep the database running and pause the API hosts before applying pending migrations. Back up local data you need to retain. Use the fresh setup above for a new database. The local command runs independently of GitHub Actions and the Azure deployment setting.

## Production tenant boundary

Before routing cloud traffic, configure the central API DNS names in `Tenant__CentralApiHosts__0` and any additional indexed entries. Include both browser-facing and server-to-server API names where applicable. Customer vanity hosts come from the active workspace registry. Set `TrustedForwardedHeaders__Proxies__0` or `TrustedForwardedHeaders__Networks__0` for the immediate trusted proxy addresses or CIDRs. Loopback is trusted by default. Validate that the ingress overwrites incoming forwarded headers and limits direct access to the API origin.

Review existing administrator grants before upgrading a database that used the System Administrator role shortcut. Preserve verified platform staff and give legitimate workspace administrators explicit tenant grants. Rerunning the configured bootstrap seeder grants permissions to the verified bootstrap account only. It does not promote members by role name. Existing users must have a tenant-bound access token with a session ID. Active roles and grants are reevaluated on protected API requests.

Quartz jobs queued by this version record their originating tenant. For older jobs with no tenant field, reconcile queued items before upgrading. Set `BackgroundJobs__LegacyTenantId` only when all outstanding legacy jobs belong to one known workspace. Otherwise requeue them in their verified workspace scopes.

Complete the two-workspace ingress, IAM and data-isolation checks before onboarding production customers. See [the tenancy readiness gate](../architecture/saas-multitenancy-strategy.md#readiness-gate).

## Invite a first workspace administrator

Use the customer workspace onboarding flow for the initial administrator. It requires KYC approval, completed provisioning, explicit invitation acceptance, and authenticator verification. See [secure workspace onboarding](../architecture/secure-workspace-onboarding.md) for setup and Microsoft and Google configuration.

## Invite an independent account

Create an account in IAM account administration with selected roles and an explicit initial password. Activate the account, then use Send Invitation. Email delivery must be configured; NoOp delivery deliberately cannot report invitation success. The recipient follows the protected, expiring link and chooses a password. Acceptance confirms email, replaces the initial password, clears the forced password-change flag and revokes prior sessions/refresh tokens. Reusing the link fails. Activation alone preserves the existing password and email confirmation state.

Activation is administrative approval (`IsActive=true`); it preserves credentials, email verification and roles, records the actor and cleans old access. Sending the invitation does not activate the account or sign in the recipient. Acceptance proves possession of the email link, sets the recipient's chosen password, confirms email and revokes that account's prior sessions. A fresh active-but-unverified account cannot sign in until setup is completed. Assigned roles and permissions determine what the recipient can view after signing in.

Acceptance deliberately preserves any other account already signed in in the browser. The completion screen identifies that account and offers an explicit **Sign out and sign in** action. For administrator testing, use a private browser window to sign in as the invited user without disturbing the administrator session. Clicking sign-in in a browser that still has an administrator session otherwise returns to that administrator's workspace.

The invitation URL must use the clone's EmailSettings:ClientBaseUrl. Configure the Blazor AcceptInvitation and IAM UserInvite backend endpoints consistently. LocalMailpit captures local email; no credentials are sent by email. ResetExistingAdminPassword remains an explicit bootstrap operation, separate from account invitation.

## Find the local administrator credentials

The sign-in page does not advertise a fixed administrator identity. In Visual Studio, right-click the API project and select **Manage User Secrets**. Use `DevelopmentSeed:AdminUserName` or `DevelopmentSeed:AdminEmail` with `DevelopmentSeed:AdminPassword` for a newly created bootstrap account. These are API settings; Web user-secrets do not define the bootstrap account. Existing accounts retain their password on ordinary bootstrap reruns unless the explicit reset option is enabled.

An account or passkey from another application database is not imported into a fresh clone. Sign in with this clone's configured administrator, complete required MFA enrollment, and register a new passkey for the account and origin before using passkey sign-in.

## Configure optional payment providers

Run `./scripts/initialize-payment-settings.ps1` to add every configurable Stripe and M-Pesa field to API user-secrets. Local platform setup invokes it automatically. Existing values are preserved; newly added credential fields are blank, M-Pesa uses Sandbox, and the default provider is NoOp. Fill provider-issued values through **ST.Api → Manage User Secrets**. See [the payment configuration checklist](environment-configuration-checklist.md) for the exact callback routes and public API origin requirements. The helper does not issue payments or register callbacks.
