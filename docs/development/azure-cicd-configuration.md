# Azure CI/CD Configuration

This guide configures the StarterTemplate GitHub Actions deployment pipeline to use Microsoft Entra workload identity federation (OIDC). GitHub receives short-lived Azure credentials for each workflow run; no client secret or App Service publish profile is stored in GitHub.

## Identity Boundaries

StarterTemplate deliberately uses two identities:

- **GitHub deployment identity:** runs EF migration bundles, manages the temporary Azure SQL firewall rule, and deploys the API/UI packages.
- **App Service managed identity:** is used by the deployed application at runtime for Key Vault, Blob Storage, Redis, SQL, and other Azure services.

Do not assume the App Service managed identity is available on a GitHub-hosted runner. It is scoped to the App Service resource.

## 1. Create The GitHub Deployment Identity

In the Azure portal:

1. Open **Microsoft Entra ID**.
2. Open **App registrations** and select **New registration**.
3. Name it, for example, `github-startertemplate-production`.
4. Leave the redirect URI empty and complete the registration.
5. Record the **Application (client) ID** and **Directory (tenant) ID**.
6. Open **Certificates & secrets** -> **Federated credentials** -> **Add credential**.
7. Choose **GitHub Actions deploying Azure resources**.
8. Select the repository owner and repository.
9. Set **Entity type** to `Environment`.
10. Set the environment name to `azure-prod`.

The environment name must match `environment: azure-prod` in `.github/workflows/deploy-azure.yml`.

## 2. Assign Azure RBAC Roles

Assign the deployment identity only the roles needed by the workflow:

- On each target App Service: `Website Contributor`.
- On the Azure SQL logical server: `SQL Server Contributor` so the workflow can create and remove its temporary firewall rule.
- If package deployment needs access to other deployment resources, grant the narrowest resource-scoped role that satisfies it.

Avoid subscription-wide `Contributor` unless it is a temporary bootstrap measure.

## 3. Create The Azure SQL Database User

Configure a Microsoft Entra administrator for the Azure SQL logical server, then connect to the target database using that administrator and run:

```sql
CREATE USER [github-startertemplate-production] FROM EXTERNAL PROVIDER;
ALTER ROLE db_ddladmin ADD MEMBER [github-startertemplate-production];
ALTER ROLE db_datareader ADD MEMBER [github-startertemplate-production];
ALTER ROLE db_datawriter ADD MEMBER [github-startertemplate-production];
```

The user name normally matches the app registration display name. If duplicate display names exist in the tenant, use the Azure SQL `WITH OBJECT_ID` form supported by your server version or rename the registration to a unique value.

`db_ddladmin`, `db_datareader`, and `db_datawriter` are the baseline migration permissions. Review generated migrations before release and tighten permissions further if organizational policy requires a custom migration role.

## 4. Configure The GitHub Environment

In GitHub, open **Settings** -> **Environments** -> **azure-prod**.

Add these environment variables:

| Variable | Example | Purpose |
| --- | --- | --- |
| `AZURE_CLIENT_ID` | Application client ID | OIDC deployment identity |
| `AZURE_TENANT_ID` | Entra tenant ID | OIDC tenant |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID | Deployment subscription |
| `AZURE_RESOURCE_GROUP` | `rg-startertemplate-prod` | Resource group containing Azure SQL |
| `AZURE_SQL_SERVER_NAME` | `bt-prod-sqlserver` | SQL logical server name without `.database.windows.net` |
| `AZURE_SQL_NETWORK_MODE` | `PublicRunner` | `PublicRunner` or `PrivateRunner` migration networking strategy |
| `AZURE_API_APP_NAME` | `starter-template-api-dev` | Exact API App Service resource name (for app-service) |
| `AZURE_UI_APP_NAME` | `starter-template-web-dev` | Exact UI App Service resource name (for app-service) |
| `AZURE_DEPLOYMENT_TARGET` | `aca-ghcr` | Target on main pushes; manual dispatch selects app-service, aca-acr, or aca-ghcr. |
| `AZURE_CONTAINER_REGISTRY_NAME` | `acrstartertemplate` | ACR Name for container deployments (for aca-acr) |
| `AZURE_ACA_API_NAME` | `ca-starter-template-api` | Exact API Container App resource name (for ACA flows) |
| `AZURE_ACA_UI_NAME` | `ca-starter-template-blazor` | Exact UI Container App resource name (for ACA flows) |
| `AZURE_SERVICE_BUS_NAMESPACE` | `bt-prod-servicebus` | Service Bus namespace for messaging |

Add this environment secret:

| Secret | Value |
| --- | --- |
| `AZURE_SQL_CONNECTION_STRING` | Passwordless Azure SQL connection string shown below |

Recommended connection string:

```text
Server=tcp:<sql-server>.database.windows.net,1433;Initial Catalog=<database>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Default;
```

The connection string contains no password. `azure/login@v2` establishes the GitHub identity, and `DefaultAzureCredential` resolves the Azure CLI credential created by that login.

Add required reviewers to the `azure-prod` environment if deployment approval is required.

## 5. Azure SQL Networking

The workflow supports two explicit networking modes:

- `PublicRunner`: GitHub-hosted runner using a temporary `/32` Azure SQL firewall rule.
- `PrivateRunner`: self-hosted runner with private network access; no firewall rule is created.

In `PublicRunner` mode the workflow:

1. Discovering the GitHub runner's public IP.
2. Creating a narrowly scoped temporary server firewall rule.
3. Running all four migration bundles.
4. Removing the firewall rule with `if: always()`.

The deployment identity therefore needs `SQL Server Contributor` on the logical server.

For a private-endpoint-only production architecture, replace the GitHub-hosted migration job with a self-hosted runner inside the Azure virtual network. Do not enable broad public access merely to make CI pass.

## 6. Migration Bundle Behavior

The workflow:

- Builds self-contained Linux migration bundles for IAM, HR, Shared, and Banking.
- Passes the Azure SQL connection string explicitly to each bundle.
- Exposes that same secret through the context-specific `ConnectionStrings__*` environment keys. EF constructs each design-time `DbContext` before processing the bundle's `--connection` override, so both forms are required.
- Retries bundle execution while a serverless Azure SQL database resumes.
- Fails immediately when a bundle reports a deterministic missing-connection configuration error instead of treating it as an Azure SQL wake-up delay.
- Uses a separate migrations history table for each context:
  - `__EFMigrationsHistory_IAM`
  - `__EFMigrationsHistory_HR`
  - `__EFMigrationsHistory_Shared`
  - `__EFMigrationsHistory_Banking`

Migration retries are safe because EF migration bundles are idempotent with respect to their migrations history table. Generated migrations must still be reviewed for operations that are not transaction-safe.

## 7. Deployment Behavior

The API and UI deployment jobs authenticate independently with OIDC because each GitHub job runs on a fresh runner. `azure/webapps-deploy@v3` then deploys to the App Service names supplied by GitHub environment variables.

The old publish-profile secrets are no longer required:

- `AZURE_API_PUBLISH_PROFILE`
- `AZURE_UI_PUBLISH_PROFILE`

Remove them after the first successful OIDC deployment.

## Troubleshooting

- `DefaultAzureCredential failed`: confirm `azure/login@v2` succeeded and the federated credential subject targets the `azure-prod` GitHub environment.
- `Login failed for user <token-identified principal>`: create the deployment identity as a contained user in the target Azure SQL database and grant migration roles.
- SQL error `40613`: the database is unavailable or resuming. The workflow retries; inspect Azure SQL status if all retries fail.
- Azure SQL Free Limit databases enforce auto-pause and do not allow `AutoPauseDelay=-1`. Keep migration retries for this tier. Move a real production workload to a paid always-on/serverless configuration when cold-start latency is unacceptable.
- SQL firewall denial: confirm public network access is enabled for this workflow model and that the deployment identity has `SQL Server Contributor`.
- `A valid connection string was not found`: confirm the migration step maps `AZURE_SQL_CONNECTION_STRING` to `ConnectionStrings__IamConnection`, `ConnectionStrings__HrConnection`, `ConnectionStrings__SharedConnection`, `ConnectionStrings__BankingConnection`, and `ConnectionStrings__DefaultConnection`. The explicit `--connection` argument alone is not available early enough for design-time context construction.
- `Resource ... not found` during deployment: verify `AZURE_API_APP_NAME`, `AZURE_UI_APP_NAME`, subscription, and tenant values.
- OIDC subject mismatch: the federated credential must target environment `azure-prod`, not a branch subject, because the workflow jobs use a GitHub environment.

## Pause and resume Azure operations

Use the repository variable AZURE_DEPLOYMENT_ENABLED as the reversible switch. It must be exactly true to permit migrations, firewall changes, Azure deployment and ACR publishing. False, missing or any other value pauses those jobs for both main pushes and manual dispatch. Keep this switch at repository scope; an azure-prod environment variable of the same name takes precedence and must not silently override it.

The subscription is currently disabled, so the repository switch is false. Build/test, artifact generation, migration-bundle generation and GHCR publishing continue. No Azure login is attempted by paused jobs. GHCR publishing still requires selecting aca-ghcr; when aca-acr is selected, the entire Azure-backed publishing job is skipped.

Pause:

    gh variable set AZURE_DEPLOYMENT_ENABLED --body false

After subscription access and target configuration are verified, resume:

    gh variable set AZURE_DEPLOYMENT_ENABLED --body true

Then run the workflow manually with the intended target or merge the next change to main. Existing failed runs remain in history; changing the variable does not retroactively alter them or cancel already-running jobs. The workflow summary reports the resolved pause/enabled state.
