# Downstream application configuration and release checklist

Use a fresh copy of this checklist for every application, environment and deployment stamp. Record actual values in the application's private configuration inventory; record secret names and owners here, never secret values. Renaming is the first step, not the complete setup.

## Release record

| Confirm | Record |
| --- | --- |
| Source | StarterTemplate commit, downstream repository and release commit |
| Target | Application name, environment, provider, pooled/dedicated stamp ID |
| Addresses | Native API/UI ports, Docker API/UI ports, public API/UI origins, internal API address |
| Owners | Application owner, identity/provider owner, infrastructure owner, reviewer/date |
| Evidence | Build/test run, browser acceptance record, backup/restore record, outstanding exceptions |

## 1. Identity before first run

- [ ] Run `scripts/rename-template.ps1 -NewName <ProductName> -NamespacePrefix <PREFIX> -WhatIf`, inspect the plan, then run without `-WhatIf`. Confirm namespace, project/solution paths, product branding (including logo initials and fallback avatars), image names and telemetry names.
- [ ] Set `origin` to the downstream repository. Confirm its owner/name, immutable repository ID and default branch. Never push downstream changes back to StarterTemplate accidentally.
- [ ] Confirm the API and Blazor `UserSecretsId` values are fresh, distinct from each other and distinct from other applications. The rename script regenerates IDs; it does not copy secret contents.
- [ ] Keep `.env`, user-secrets, certificates, local key rings and generated logs out of Git and Docker build contexts. Start from empty provider credentials rather than another application's secrets.
- [ ] Choose a stable `DataProtection:ApplicationName` before creating accounts or issuing protected data. Keep it unchanged after first use; later renaming requires a deliberate migration or security reset.

## 2. Workstation and container isolation

- [ ] Allocate native HTTP/HTTPS ports for both hosts in `Properties/launchSettings.json`. Update the UI's API addresses and integration callback registrations accordingly.
- [ ] Allocate different Docker host ports for API/UI. Keep these distinct from native ports if both modes can run together. Container-internal ports remain unchanged.
- [ ] Review **every** infrastructure host mapping in `ops/local/docker-compose.yml`: SQL Server, RabbitMQ/management, Redis, Mailpit SMTP/UI, Seq and all three Azurite ports. Renaming does not allocate ports.
- [ ] Confirm a distinct Compose project name, all explicitly named external volumes, the Blazor key volume, cache/key-ring prefixes, storage account/container prefixes and local profile-image paths. Do not reuse another application's data volumes.
- [ ] Create ignored `ops/local/.env` with this application's credentials and port overrides. Required API container values include `APP_SQL_CONNECTION_STRING` and `APP_JWT_SECRET`. Optional SSO values use the `APP_GOOGLE_SSO_*` and `APP_ENTRA_SSO_*` mappings already present in the apps Compose file; an extra stdin override is unnecessary.
- [ ] Run the endpoint audit with **both** new Compose files before startup:

```powershell
.\scripts\check-local-endpoints.ps1 -ComposeFiles @('ops/local/docker-compose.yml', 'ops/local/docker-compose.apps.yml')
```

- [ ] Document any deliberate shared local infrastructure. Use separate application databases and key/cache namespaces even on a shared server. Sharing a server is not evidence of independent infrastructure certification.

## 3. Configuration sources and URLs

- [ ] Confirm the effective configuration in each host: JSON defaults, Development JSON, API/UI user-secrets, then environment overrides. Docker containers consume Compose environment values; they do not automatically read Windows user-secrets.
- [ ] Keep local secrets in user-secrets or ignored local environment files; deployed secrets in the host's approved secret store. Environment names use `Section__Setting`; Key Vault names use `Section--Setting`.
- [ ] Set Blazor `BackendApi:BaseUrl` to its server-to-server API address. Set `BackendApi:PublicBaseUrl` to the API origin reachable by the browser. Docker service names are internal addresses, not browser callback origins.
- [ ] Set API `ExternalSignIn:PublicApiOrigin`, each `ExternalSignIn:ClientOrigins` entry, `AllowedOrigins` and `EmailSettings:ClientBaseUrl` to the intended API/UI origins. Remove obsolete template origins.
- [ ] Review `AllowedHosts`, `Tenant:CentralApiHosts`, `Tenant:DeploymentStampId`, and `TrustedForwardedHeaders:Proxies`/`Networks` for the actual ingress. Include the API service DNS name used by Blazor (for example `api`) in the central host list alongside the public API host. Trust only the immediate configured proxies; ensure the ingress overwrites client-supplied forwarded headers and direct API access is appropriately restricted.

## 4. Database, bootstrap and tenant policy

- [ ] Select `DatabaseSettings:Provider` and configure `ConnectionStrings:DefaultConnection`. Confirm any `ControlPlaneConnection`, `IamConnection` and `SharedConnection` overrides deliberately; a dedicated stamp uses central ControlPlane plus its own IAM/Shared data database.
- [ ] Use a new application database. Apply the provider-specific ControlPlane, IAM and Shared migrations in that order, including the Shared Quartz schema. Do not apply InitialStarter baselines to an unrelated existing database.
- [ ] Restore pinned local tools using `dotnet tool restore`; do not upgrade EF or Quartz majors as a setup workaround. Check provider histories and pending model changes.
- [ ] For an existing application database, run `scripts/update-local-database.ps1` before using `-Apply`. Pause API writers and retain a backup before upgrades. This local workflow requires matching context connections and refuses remote/system databases.
- [ ] Confirm the bootstrap tenant ID matches `Tenant:DefaultTenantId` and the intended seeded workspace. Identical seed IDs in separate application databases do not share authority; do not mechanically replace persisted IDs.
- [ ] If deliberately bootstrapping locally, set `DevelopmentSeed:AdminUserName`, `AdminEmail`, `AdminPassword` and `TenantId` in the new API's secret store, then enable `DevelopmentSeed:Enabled`. `GrantPlatformAccess` is an explicit vendor bootstrap choice. Keep `ResetExistingAdminPassword` disabled. Disable bootstrap after setup and remove its password when no longer required.
- [ ] Confirm platform staff identity, explicit operation permissions, tenant delegation, essential Core/IAM entitlements and administrator MFA. Role names must never grant platform authority.
- [ ] Keep customer KYC mandatory. Record actual reviewer/evidence for production customers before provisioning, invitation delivery/acceptance or activation. Label synthetic acceptance fixtures clearly.
- [ ] Send expiring administrator invitations through the workspace setup flow. Never configure a shared temporary administrator password or use password recovery as initial onboarding.

## 5. Authentication, Data Protection and sessions

- [ ] Generate a unique strong `JwtSettings:SecretKey`; review `Issuer`, `Audience`, token lifetimes and clock skew. An isolated runtime requires its own signing key, issuer and audience.
- [ ] Preserve API Data Protection keys across restarts and replicas. When native and Docker API hosts use the same database, configure the shared key ring intentionally, for example local Redis via the setup script's `-UseSharedLocalKeyRing` option. A persistent path in one host is not automatically shared with another host.
- [ ] Review `DataProtection:UseExternalKeyStore`, key-ring location, Redis key/connection or Blob URI, and `KeyEncryptionMode`. Confirm access to the encryption key as well as the stored key ring. Use the production store/encryption policy; never rely on ephemeral container files.
- [ ] Give Blazor a persistent key ring appropriate to its replicas so protected browser session storage survives host replacement. Keep bearer/refresh tokens out of Redis.
- [ ] Review `SecuritySettings:Mfa`, `SecuritySettings:SessionSettings` and Blazor `SessionLifecycle`. Verify idle warning, refresh, logout, revocation and clean sign-in after suspension/reactivation.

## 6. Google, Microsoft and passkeys

For the current server-side sign-in flow, provider redirects terminate on the **API**, not Blazor. These are unrenamed StarterTemplate examples; replace the ports/origins for a downstream application.

| Runtime | Google redirect | Microsoft Web redirect |
| --- | --- | --- |
| Visual Studio HTTPS | `https://localhost:7229/signin-google` | `https://localhost:7229/signin-oidc` |
| Visual Studio HTTP | `http://localhost:5257/signin-google` | `http://localhost:5257/signin-oidc` |
| Docker HTTP | `http://localhost:8180/signin-google` | `http://localhost:8180/signin-oidc` |
| Deployed | `https://<public-api>/signin-google` | `https://<public-api>/signin-oidc` |

- [ ] Create an application/environment-owned Google **Web application** client. Set `GoogleSso:Enabled`, `ClientId`, `ClientSecret` and the actual callback path. Register exact redirect URIs; no Authorized JavaScript origins are needed for the shipped server-side OIDC flow. Review intended audience and consent requirements. See [Google's web-server guide](https://developers.google.com/identity/protocols/oauth2/web-server).
- [ ] Create an application/environment-owned Microsoft registration with **Web** redirects. Set `EntraId:Enabled`, `TenantId`, `ClientId` and the client secret **value**, with an expiry/rotation owner. Select the intended supported account population; the documented baseline uses a specific directory/single-tenant registration. Wider account audiences require matching authority/issuer behavior and explicit acceptance checks. A Microsoft directory tenant is separate from an application customer workspace.
- [ ] Use HTTPS for deployed callbacks. HTTP localhost is for local testing. Follow [Microsoft's redirect restrictions](https://learn.microsoft.com/entra/identity-platform/reply-url), including its localhost matching behavior; do not assume a distinct localhost port creates an isolated Microsoft registration boundary.
- [ ] For every new stamp origin, update registered redirects, approved UI return origins and provider credentials before enabling buttons. Verify real provider-account login and explicit account connection; matching email alone must not connect accounts.
- [ ] If enabling passkeys, configure `Passkeys:Enabled`, `ServerDomain`, `ServerName` and `Origins` for the actual relying party. Test with a real authenticator separately; backend challenge tests do not certify a physical device.

## 7. Runtime providers

- [ ] Review `EmailSettings:Provider`, sender, client base URL and provider credentials. Mailpit is local capture only; production delivery uses an approved API adapter. Verify invitation and recovery links reach the correct UI.
- [ ] Configure `CacheSettings:Provider` and credentials/namespaces; test expiry and invalidation between two customers.
- [ ] Configure `Messaging:Enabled`, `Messaging:Transport` and transport credentials, namespace/vhost, queues and worker deployment. Verify outbox delivery and duplicate handling. A test with messaging disabled does not certify transport delivery.
- [ ] Review `BackgroundJobs:Enabled`, Quartz database history and outstanding jobs. Set `LegacyTenantId` only after proving every legacy item belongs to that workspace; otherwise reconcile/requeue them correctly.
- [ ] Configure private `ProfileImageStorage` roots/containers, encryption/access identities and retention. Verify authorized API access and foreign-customer denial; do not expose private blob URLs.
- [ ] Keep optional providers disabled until configured. Review payment/SMS/reporting license settings, feature flags, entitlements, provider callback origins and health probes for the capabilities actually shipped downstream. `NoOp` is a local test choice, not proof of live delivery/payment processing.
- [ ] Rename observability service/namespace, log destinations and telemetry resources. Confirm logs exclude credentials, tokens and connection strings.

## 8. CI/CD and dedicated stamps

- [ ] Replace repository-specific workflow environments, image registry owner/path, resource prefixes/groups, subscription/region, DNS/TLS and provider defaults. The rename script cannot create these resources or authorize identities.
- [ ] Keep the repository variable `AZURE_DEPLOYMENT_ENABLED=false` until the target is deliberately ready. Check environment-level overrides too. Confirm other deployment workflows' switches before enabling them. Build/test and packaging success alone does not prove deployment.
- [ ] Configure GitHub-to-Azure OIDC federation, managed identities and least required resource/Key Vault/database access. Keep long-lived Azure client secrets and publish profiles out of deployment workflows.
- [ ] For isolated provisioning, configure `ControlPlane:GitHub` dispatch settings, `ControlPlane:Provisioning:Enabled`, callback `Authentication:Enabled`, `Repository`, immutable `RepositoryId`, `Subject`, `Ref` and `Audience`. Match the actual downstream repository and protected environment, not StarterTemplate's identity. Follow [the provisioning configuration table](../architecture/tenant-onboarding-workflow.md#configuration).
- [ ] Confirm callback HTTPS origin/audience, central ControlPlane connection, registry pulls, secret ownership and the dedicated runtime's stamp binding. Provision one workspace per isolated stamp.
- [ ] Exercise live deployment, signed success, reported failure, interrupted-run timeout, retry, duplicate/stale callbacks, and originating-workspace invitation delivery.
- [ ] Rehearse backup/restore and cutover/rollback using the deployment's real data, Data Protection key access, private assets and queued work. Record measured recovery objectives. Follow [backup/restore](../architecture/tenant-backup-restore.md) and [stamp migration](../architecture/stamp-migration-runbook.md).

## 9. Acceptance and sign-off

- [ ] Build both renamed hosts, run unit/architecture/provider integration checks and verify migration histories from the selected revision.
- [ ] Start with fresh independent application resources; verify API health, UI assets and real credential sign-in.
- [ ] Walk through two customer workspaces in separate sessions: KYC, invitation acceptance, password/provider sign-in, MFA, own data/roles/navigation, foreign-workspace denial and direct-route/API authorization.
- [ ] Request scoped support, approve it as the correct customer, verify read-only/write limits, revoke it, then verify existing access and refresh are denied.
- [ ] Suspend a customer with an active session; verify access/refresh denial and no bypass through another host/selector. Reactivate and verify old sessions stay invalid and fresh sign-in succeeds. Confirm the second customer remains unaffected.
- [ ] Record real Google/Microsoft and physical-passkey acceptance separately when enabled. Keep user credential/device steps with the account owner when required.
- [ ] Record exactly which checks used a browser, normal HTTP APIs, simulated providers or deployed services. Record pending cloud/recovery checks explicitly before customer onboarding.
