# Tenant Onboarding Workflow

This document outlines the end-to-end operational workflows for onboarding new tenants to the StarterTemplate SaaS platform. Because the platform supports both **Pooled** and **Isolated** isolation tiers, the onboarding steps vary based on the level of isolation required by the client.

## 1. Onboarding a Pooled Tenant

A pooled tenant shares compute, database, and caching infrastructure with other pooled tenants. Data is logically isolated via row-level security and EF Core Global Query Filters. This is the fastest and cheapest way to onboard a new customer.

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
   - The tenant is immediately active. When users for this tenant log in, the application routes their traffic to the shared infrastructure and uses their `TenantId` to filter data automatically.

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
| **Data Isolation** | Logical (Row-Level Security, EF Filters). | Physical (Dedicated Database). |

---

## 3. Post-Provisioning Activation Runbook (Isolated Tenants)

After the `provision-isolated-stamp.yml` GitHub Actions workflow completes, the tenant record is still in `Provisioning` status. The provisioning workflow cannot automatically activate the tenant because the API does not expose a public inbound webhook for GitHub Actions callbacks. Activation is a deliberate operator gate.

### Steps

1. **Confirm the GitHub Actions workflow succeeded.**
   - Navigate to the repository → **Actions** → **Provision Isolated Stamp**.
   - Confirm the run for the correct `stamp_id` shows a green ✅.

2. **Retrieve the Key Vault URI from the Bicep deployment output.**
   - In Azure Portal → Resource Group → Deployments → the latest `stamp-isolated` deployment.
   - Copy the `keyVaultUri` output value (e.g., `https://kv-{stamp-id}-prod.vault.azure.net/`).

3. **Update the DeploymentStamp with the Key Vault URI.**
   - Call `PUT /api/v1/control-plane/stamps/{stampId}` with the Key Vault URI.
   - This allows the application to resolve per-tenant secrets at runtime via Managed Identity.

4. **Activate the tenant.**
   - Call `POST /api/v1/control-plane/tenants/{tenantId}/activate`.
   - The tenant status transitions to `Active`.
   - `TenantResolutionMiddleware` will now resolve the tenant for incoming requests on the provisioned hostname.

5. **Verify end-to-end.**
   - Make a test request to the tenant's hostname and confirm the correct tenant ID is resolved.
   - Confirm the connection interceptor routes to the isolated database (check logs for `TenantDbMetadata_<tenantId>` cache key).

---

## 4. ProvisioningFailed Recovery

If the `provision-isolated-stamp.yml` workflow fails, or the `GitHubActionsStampProvisioner` cannot dispatch the workflow (e.g., expired PAT, misconfigured repo name), the tenant is automatically set to `ProvisioningFailed` status.

### Recovery Steps

1. **Diagnose** — Check the API logs for the `Stamp provisioning dispatch failed` error entry. Confirm the GitHub Actions credentials (`ControlPlane:GitHub:Pat`, `RepoOwner`, `RepoName`) are correctly set.

2. **Fix the root cause** — Update the configuration (e.g., rotate the PAT in `dotnet user-secrets` or Key Vault).

3. **Retry provisioning** — Call `POST /api/v1/control-plane/tenants/{tenantId}/activate`. The activate handler transitions the status to `Active` (for manual/already-provisioned cases). If infrastructure was never created, run the provisioning workflow manually from GitHub Actions UI before calling activate.

4. **Alternatively, provision manually** — Navigate to GitHub Actions → **Provision Isolated Stamp** → **Run workflow**, provide `stamp_id`, `resource_group`, and `database_provider`, then follow the Post-Provisioning Activation Runbook above.
