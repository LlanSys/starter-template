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

- [ ] Preserve the stable .NET 10 policy, central Directory.Packages.props and lowercase global.json. Run scripts/check-package-policy.ps1 after renaming and after dependency changes. Keep project package references unversioned.
- [ ] After the first successful hosted Dependency Policy run, require its `.NET 10 and dependency policy` check in the downstream branch ruleset and confirm Dependabot alerts/security updates. Review the grouped update PRs; see [package management](package-management.md).

## 2. Workstation and container isolation

- [ ] Allocate native HTTP/HTTPS ports for both hosts in `Properties/launchSettings.json`. Update the UI's API addresses and integration callback registrations accordingly.
- [ ] Allocate different Docker host ports for API/UI. Keep these distinct from native ports if both modes can run together. Container-internal ports remain unchanged.
- [ ] Set every infrastructure `*_HOST_PORT` override in the ignored `ops/local/.env`, using the keys in `.env.example`. Include SQL Server, RabbitMQ/management, Redis, Mailpit SMTP/UI, Seq, all three Azurite ports and ClamAV. The setup script carries these values into native host settings.
- [ ] Confirm a distinct Compose project name, all explicitly named external volumes, the Blazor key volume, cache/key-ring prefixes, storage account/container prefixes and local profile-image paths. Do not reuse another application's data volumes.
- [ ] Create ignored `ops/local/.env` with this application's credentials and port overrides. Required API container values include `APP_SQL_CONNECTION_STRING` and `APP_JWT_SECRET`. Optional SSO values use the `APP_GOOGLE_SSO_*` and `APP_ENTRA_SSO_*` mappings already present in the apps Compose file; an extra stdin override is unnecessary.
- [ ] Run the endpoint audit with **both** new Compose files before startup:

```powershell
.\scripts\check-local-endpoints.ps1 -FocusSupplied -ComposeFiles @('ops/local/docker-compose.yml', 'ops/local/docker-compose.apps.yml')
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
- [ ] Use a new application database. The local setup scripts derive the SQL name from the renamed API prefix, or use `LOCAL_DATABASE_NAME` when supplied. Apply the provider-specific ControlPlane, IAM and Shared migrations in that order, including the Shared Quartz schema. Do not apply InitialStarter baselines to an unrelated existing database.
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

- [ ] Configure KycDocuments for the environment: private container, API managed identity in Azure, reviewer permissions, link lifetime and upload limits. Verify client submission, platform upload/review, wrong-workspace denial, expiry/revocation and closure after KYC approval. Confirm document retention and processing policy. Follow [KYC documents](kyc-documents.md).

- [ ] Repeat the [platform IAM matrix](platform-iam-acceptance.md) for downstream roles/grants and verify browser navigation plus direct API denial. Do not assume control_plane.manage provides a read-only or KYC-only role.

## KYC retention

- Enabled general-business default: active relationship +24 months after explicitly recorded end; unsuccessful closure 90 days; unfinished inactivity 30 days; recovery 30 days; orphan grace 7 days; hold review 90 days.
- Use Control Panel → KYC Retention Policy to publish a downstream-specific version. Existing cases remain pinned until explicitly changed. Regulated products must choose the policy appropriate to their activity.
- Confirm separate private `KycDocuments:ContainerName` and `KycDocuments:ErasureLedgerContainerName`, list/read/write/delete access for originals, and read/create access for the independent erasure ledger.
- Set the cloud backup/export maximum to 35 days for this deployment profile, preserve the erasure ledger beyond every recoverable backup, and verify restore reconciliation before exposing restored data. Cloud certification remains pending.
- Use independent `control_plane.*` operation grants; retired `control_plane.manage` is never a wildcard. See [platform IAM acceptance](platform-iam-acceptance.md) and [retention operations](kyc-retention-policy.md).

## Document-specific KYC collection

- [ ] Confirm `KycDocuments:DocumentTypes` matches the product's requested evidence and avoid collecting unnecessary documents.
- [ ] Keep catalogue keys stable; add future types through API configuration and restart/redeploy.
- [ ] Apply the provider-specific `ClassifyKycDocuments` migration and deploy API/UI together.
- [ ] Verify a standard-type upload, named Other upload, duplicate retry and client/platform visibility in the deployment.
- [ ] Review earlier `legacy` uploads explicitly; classification is never guessed during migration.

See [KYC documents](kyc-documents.md#document-specific-collection) for the contract, configuration and upgrade behavior.

## Shared administration UI

- [ ] Confirm branding and the shared MudProviderHost palette, including neutral table headers, consistent primary actions and semantic status colors.
- [ ] Use the Access Catalog header/tab/filter/table/pager pattern for new administration slices; omit repeated headings in embedded tabs.
- [ ] Set wide table data using `--st-table-min-width`, keeping paging controls inside the viewport.
- [ ] Check platform and tenant permission-limited views, desktop, tablet and phone layouts.
- [ ] Keep template-only routes out of product navigation; legacy redirects preserve old links without shipping example screens.

The detailed contract and local acceptance evidence are in [UI conventions](ui-conventions.md).

## Operational task ownership

- [ ] Apply the provider-specific ControlPlane task-assignment/history and IAM assignment-permission and managed-role upgrade migrations through the normal upgrade workflow.
- [ ] Confirm Core / Tasks page availability and IAM availability for customer support approvals.
- [ ] Select platform team leads for `control_plane.manage_tasks` and customer team leads for `tasks.assign`; ensure each also has the underlying task action permissions. Review existing custom roles explicitly.
- [ ] Confirm assignees' workspace, active/email/setup/MFA state and effective permissions. Verify an assignment is rejected after removing a target permission, including after the dropdown has loaded.
- [ ] Verify claim/assignment moves one item out of Unassigned into the selected account's My Tasks; verify release, manager reassignment, concurrent-claim rejection, source completion/history and new-attempt isolation.
- [ ] Confirm the completion reconciler is healthy and All Team Tasks highlights an assignee who loses eligibility. Preserve history and the existing customer IAM database routing; assignment does not grant or exclusively lock workflow authority.

## Account admission and evidence safety

- [ ] Confirm MaxUsers policy: non-deleted humans include inactive/setup accounts; zero is unlimited. Keep proxy kind server-owned and preserve the tenant-scoped identity indexes and transactional admission store.
- [ ] Allocate a distinct ClamAV host port, signature volume and private endpoint. Configure `KycDocumentSafety` in each runtime; preserve UTC, encrypted/limit alerts, freshness checks and fail-closed gates.
- [ ] Verify synthetic clean/unsafe/encrypted uploads, scanner outage/recovery, reviewer permissions and legacy evidence scan backlog. Confirm no public originals and no manual clean bypass.
- [ ] Use the guarded upgrade workflow with writers paused. Resolve same-workspace duplicate emails explicitly before the new unique index. See [upgrade and safety runbook](account-admission-and-kyc-safety.md).

## Platform staff and shared request cache

- [ ] Apply both-provider staff/tenant-settings permission upgrade migrations using the normal guarded database workflow. Confirm a usable existing staff manager before upgrade; limited roles and label-only administrators are not promoted.
- [ ] Establish at least two verified platform administrators with independent owned authenticators. Create real operators through Platform Staff, select least-privilege profiles and inspect saved grants/audit reasons. Keep local `qa.*` accounts and their DPAPI handoff out of deployed databases.
- [ ] Check invitation provider delivery, MFA enrollment, live revocation, last-manager protection and ordinary-IAM denial for staff targets.
- [ ] Deploy the request-v2 cache correction to every API replica together and drain old instances. Keep the shared Redis provider for multi-replica invalidation; never clear Data Protection keys as a cache maintenance action.
- [ ] Repeat cold/warm two-tenant reads and read-after-update/deletion through each replica. Verify no platform decoy or other customer value appears. See [tenant cache isolation](tenant-cache-isolation.md).
