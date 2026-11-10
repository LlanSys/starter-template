# Tenant Onboarding Workflow

This document outlines the operational workflows for onboarding new tenants to the StarterTemplate SaaS platform. See [secure workspace onboarding](secure-workspace-onboarding.md) for KYC review, invitation delivery, acceptance, and MFA. Because the platform supports both **Pooled** and **Isolated** isolation tiers, the onboarding steps vary based on the level of isolation required by the client.

## 1. Onboarding a Pooled Tenant

A pooled tenant shares compute, database, and caching infrastructure with other pooled tenants. StarterTemplate enforces logical isolation through tenant-scoped EF queries and tenant ownership checks on writes. This is the fastest and cheapest way to onboard a new customer.

### Workflow

1. **No Infrastructure Changes Needed:**
   Because pooled tenants share infrastructure, you do not need to run any Terraform or Bicep pipelines. The shared resources (e.g., `default-pooled-stamp`) already exist.

2. **Register the Tenant (Control Panel):**
   - An administrator logs into the StarterTemplate Control Panel.
   - Navigates to **Tenants** -> **Add Tenant**.
   - Fills in the tenant details (e.g., Name, Domain).
   - Selects the existing **Pooled Deployment Stamp** (e.g., `default-pooled-stamp`).
   - Saves the tenant.

3. **Provisioning Complete:**
   - The system creates the tenant record in the Control Plane database.
   - The tenant awaits KYC. A reviewer records the evidence reference and approves KYC. Approval can activate a pooled tenant on the existing stamp. Workspace routing uses its `TenantId` to scope data.

---

## 2. Onboarding an Isolated Tenant

An isolated tenant receives their own dedicated physical infrastructure. They do not share compute, databases, or key vaults with any other tenant. This tier requires provisioning new Azure resources before the tenant can be registered in the software.

### Workflow

1. **Provision Infrastructure (IaC Pipeline):**
   - The infrastructure team (or automated CI/CD pipeline) runs the Bicep/Terraform scripts to provision a new **Deployment Stamp** for the client.
   - This provisions a new Azure Resource Group, App Service / Container App, SQL Database, Redis Cache, and a dedicated **Azure Key Vault**.
   - The pipeline securely saves the connection strings (Database, Cache, etc.) into the newly provisioned Azure Key Vault using our standard convention (e.g., `ConnectionStrings--Database`).

2. **Retrieve the Key Vault URI:**
   - Once the infrastructure pipeline completes, it will output the URI of the newly created Key Vault (e.g., `https://kv-clientxyz-prod.vault.azure.net/`).

3. **Register the Deployment Stamp (Control Panel):**
   - An administrator logs into the StarterTemplate Control Panel.
   - Navigates to **Deployment Stamps** -> **Add Stamp**.
   - Creates a new stamp record representing the infrastructure provisioned in Step 1.
   - Sets the Tier to **Isolated**.
   - Inputs the **Key Vault URI** obtained in Step 2. (The application will use its Managed Identity to read the actual connection strings from this vault at runtime).

4. **Register the Tenant (Control Panel):**
   - The administrator navigates to **Tenants** -> **Add Tenant**.
   - Fills in the tenant details (e.g., Name, Domain).
   - Links the tenant to the **newly created Isolated Deployment Stamp** from Step 3.
   - Saves the tenant.

5. **Provisioning Complete:**
   - The system creates the tenant record in the Control Plane.
   - When traffic arrives for this tenant, the Control Plane resolves the Isolated Stamp.
   - The ASP.NET Core backend dynamically reads the connection strings from the tenant's isolated Key Vault and establishes a connection to their dedicated database.

---

## Summary of Differences

| Phase | Pooled Tenant | Isolated Tenant |
| :--- | :--- | :--- |
| **Infrastructure** | Uses existing shared resources. | Requires running IaC pipelines to provision dedicated resources (App, DB, Key Vault). |
| **Deployment Stamp** | Reuses an existing `Pooled` stamp. | Requires creating a new `Isolated` stamp in the Control Panel using the new Key Vault URI. |
| **Cost & Time** | Instant onboarding, lowest cost. | Slower onboarding (infrastructure deployment time), premium cost. |
| **Data Isolation** | Logical (tenant-scoped EF reads and writes). | Physical (Dedicated Database). |

---

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
