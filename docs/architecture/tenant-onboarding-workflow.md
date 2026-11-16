# Tenant onboarding workflow

The [tenant onboarding journey](tenant-onboarding-journey.md) describes sales/trial intake, KYC, provisioning and administrator setup. This runbook covers technical provisioning. See [secure workspace onboarding](secure-workspace-onboarding.md) for invitations, credentials and MFA.

## Pooled customer workflow

1. Register a customer workspace on an existing pooled stamp; record the intended initial administrator where known.
2. Collect and review KYC, then record explicit approval with evidence reference and reviewer.
3. Initialize the workspace and essential tenant-owned IAM configuration on the approved stamp. Activation and invitation delivery must retain the existing KYC gates.
4. Deliver the first-administrator invitation through the durable worker; complete credential proof and MFA before assigning administrator authority.

No new stamp infrastructure is normally required. Logical isolation still requires scoped reads/writes and independent API authorization; shared infrastructure does not make activation unconditional.

## Dedicated customer workflow

1. Register the intended isolated stamp and customer workspace without granting access. Confirm region, ownership and deployment prerequisites.
2. Collect and approve KYC before starting a customer provisioning attempt. Infrastructure preparation may be arranged in advance, but it does not satisfy the KYC or activation gates.
3. Dispatch the persisted provisioning attempt, deploy the runtime and provider schema, and verify health and destination ownership.
4. Activate only through verified completion for the current attempt. Failure/timeout enters recovery; a stale callback cannot activate a newer attempt.
5. Initialize tenant-owned IAM and deliver the administrator invitation only after readiness. Complete credentials and MFA before final access.

The workflow source and local provider checks exist. Dedicated cloud runtime/ingress, external sign-in and recovery certification remain deployment acceptance items; local database tests do not certify the entire dedicated deployment.

## 3. Isolated workspace provisioning

KYC approval persists a provisioning attempt ID and a two-hour deadline with the workspace. The dispatcher passes the workspace, attempt, registered stamp ID, resource group and provider to `provision-isolated-stamp.yml`. Each isolated stamp belongs to one workspace. A unique database reservation prevents two concurrent owners.

The workflow serializes work by stamp, publishes API and Blazor images tagged with the checked-out commit, provisions resources, applies IAM and Shared migrations, configures the runtime, deploys both applications and waits for their health checks. The isolated runtime uses the shared ControlPlane connection, its own data database, a stamp-bound tenant resolver, and its own signing key, issuer and audience. Its invitation worker initializes and delivers onboarding only for workspaces assigned to that stamp. A persisted, expiring setup lease serializes initialization across requests and replicas. Role initialization completes before invitation delivery.

The workflow reports success or failure to `POST /api/v1/control-plane/provisioning/{tenantId}/attempts/{operationId}`. This endpoint requires the dedicated GitHub OIDC policy. It checks the GitHub signature, issuer, audience, immutable repository ID, repository, exact subject, ref and workflow. Configure these claims using the [GitHub OIDC reference](https://docs.github.com/en/actions/reference/security/oidc). The separate operator endpoint requires an authenticated platform session and its operation permission.

Completion verifies the current attempt, KYC, provider, schema and destination ownership before activation. Repeating an identical completed result succeeds. Stale attempts and conflicting destinations are rejected. Failed dispatches and reported failures enter `ProvisioningFailed`. The timeout worker expires interrupted attempts. Retry retains the KYC evidence and creates a new attempt.

### Configuration

Configure these values before enabling isolated provisioning. Keep the repository variable `AZURE_DEPLOYMENT_ENABLED` false through local verification.

| Location | Setting | Purpose |
| --- | --- | --- |
| Control-plane host | `ControlPlane:Provisioning:Enabled` | Enables dispatch and timeout processing. |
| Control-plane host | `ControlPlane:GitHub:RepoOwner`, `RepoName`, `Pat` | Dispatches the workflow in the owning repository. Store the scoped credential in the host secret store. |
| Control-plane host | `ControlPlane:Provisioning:Authentication:Enabled` | Enables the machine completion policy. |
| Control-plane host | Authentication `Repository`, `RepositoryId`, `Subject`, `Ref`, `Audience` | Exact trusted GitHub claims. Configure the subject emitted for the protected environment, including any configured immutable-subject format. The default audience is `urn:startertemplate:provisioning`. |
| GitHub environment | `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_REGION` | Selects the deployment identity and location. |
| GitHub environment | `DATABASE_PROVIDER` | Matches the ControlPlane provider. Each host uses its configured provider. |
| GitHub environment | `PROVISIONING_CALLBACK_ORIGIN`, `PROVISIONING_CALLBACK_AUDIENCE` | HTTPS control-plane origin and matching OIDC audience. |
| GitHub environment | `PROVISIONING_TRUSTED_PROXY_CIDR` | Bounded network for the stamp's immediate trusted ingress. Validate the actual ingress in staging. |
| GitHub secrets | `AZURE_SQL_ADMIN_PASSWORD` | Initializes the stamp database. |
| GitHub secrets | `PROVISIONING_CONTROLPLANE_CONNECTION` | Central ControlPlane connection available to the new runtime. Prefer an explicitly authorized managed identity where the deployment supports it. |
| GitHub secrets | `GHCR_PAT` | Allows the deployed container applications to pull their private images. |
| Pooled production host | `Tenant:DeploymentStampId` | Binds the host to its registered pooled stamp. The isolated workflow supplies the corresponding binding for dedicated hosts. |

Use the protected GitHub environment `starter-azure-prod` with reviewed deployments from the configured ref. Reserve the target resource group and authorize the CI workload for resource deployment, the required role assignments, and Key Vault secret management before its first run. Its Key Vault data access must include reading the new connection secret and writing the ControlPlane connection and initial signing key. Verify that the deployed API identity can access its configured ControlPlane database.

The workflow preserves an existing signing key and stops if it cannot verify that key. Resource names derive from the registered stamp ID. Runtime settings include the generated API and client origins. Successful completion records the client origin so sign-in from the central UI can route to the owning UI. Configure Microsoft and Google registrations and credentials for the actual stamp origins when enabling those providers.

### Failure and recovery

Inspect the current workspace state and the matching GitHub run. Correct the configuration, then call `POST /api/v1/control-plane/tenants/{tenantId}/provisioning/retry`. A retry requires recorded KYC approval and a failed or expired attempt. Earlier callbacks retain their original attempt ID and cannot activate the new attempt.

The operator completion endpoint remains `POST /api/v1/controlplane/webhooks/provisioning-completed/{tenantId}`. It requires platform permission and the current operation ID. Supply a verified destination and matching provider through the protected request body.

Apply the ControlPlane migrations before deploying the updated API. The ownership migration backfills existing isolated assignments and adds a unique index. Reconcile multiple workspaces assigned to one isolated stamp before upgrading. Legacy provisioning records receive a fresh attempt through recovery.

Use the [stamp migration procedure](stamp-migration-runbook.md) for a prepared tenant-only data move and the [backup and restore procedure](tenant-backup-restore.md) for recovery. Record dataset validation, key-ring access, private assets and queued work before cutover. Local provider tests rehearse the IAM account slice, physical routing, suspension, exclusive ownership and rollback. Deployment-specific data, provider-account login, key access, ingress, backups and recovery objectives require cloud acceptance.
