# Set Up A StarterTemplate Clone

StarterTemplate contains its own application code, plans, scripts and tests. A downstream application uses the StarterTemplate revision it clones; no separate repository, source commit or extraction document is required to rename, configure, build or extend it.

The domain-neutral refactor is in progress. Banking/HR still exist while IAM is decoupled, and fresh initial migrations have not yet been generated for the final retained model. Full clone certification is the final gate in the refactor plan.

## Rename before first run

Clone StarterTemplate into the new application's directory and run its rename script from that root:

```powershell
.\scripts\rename-template.ps1 -NewName MyApplication -NamespacePrefix MA -WhatIf
.\scripts\rename-template.ps1 -NewName MyApplication -NamespacePrefix MA
```

Review the diff and set the clone's Git remote to its own repository. The script updates product/project/namespace names, Dockerfiles and MAUI XAML, and assigns fresh host UserSecretsId values. It does not copy local secret contents or configure live cloud resources. Allocate distinct host ports and Compose project/volume names before starting applications side by side.

Keep DataProtection:ApplicationName stable after the first protected cookie, token or TOTP secret is issued. See [Renaming the template](renaming-the-template.md).

## Configure the local application

Development seeding is disabled, and account passwords are blank. Configure the new API project's user-secrets with these keys before enabling bootstrap:

- ConnectionStrings:DefaultConnection: the clone's own database connection.
- JwtSettings:SecretKey: a strong random signing secret.
- DevelopmentSeed:AdminUserName, AdminEmail and AdminPassword: the clone's administrator credentials.
- DevelopmentSeed:TenantId and Tenant:DefaultTenantId: the same locally configured tenant identifier.
- DevelopmentSeed:Enabled: true only when deliberately bootstrapping that local database.
- IamProvisioning:TemporaryPassword: an independently configured temporary password for account provisioning.

Bootstrap creates one independent system administrator, seeds only the platform administrator role, and preserves passwords/lifecycle on normal reruns. It rejects tenant/identity conflicts and inactive, deleted or business-linked accounts, and never deletes unrelated users. ResetExistingAdminPassword is explicit development configuration.

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

Use fresh databases while the refactor model and migration baseline are settling. Provider integration tests, browser/email flows and optional integrations must be exercised against the clone's configuration before calling it ready.

## Deployment configuration

Each downstream repository owns its GitHub secrets/variables, environment bindings, OIDC identities and cloud targets. Azure deployment/provisioning require AZURE_DEPLOYMENT_ENABLED=true in that repository. Keep the switch false while configuring and validating new targets. Choose application-specific resource prefixes, database names and deployment environment names; regenerate compiled Bicep JSON after infrastructure changes.

StarterTemplate currently uses starter-azure-prod/starter-non-azure-prod environments and stapp/stprod resource defaults. These are templates to customize, not preconfigured cloud resources.
