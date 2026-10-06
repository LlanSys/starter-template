# Implementation Plan - GitHub Actions To Azure CI/CD

This document records the intended CI/CD architecture. The executable source of truth is `.github/workflows/deploy-azure.yml`; the required Azure and GitHub setup is documented in [Azure CI/CD Configuration](azure-cicd-configuration.md).

## Goals

- Build the API, Blazor UI, and test projects in `Release` mode.
- Run architecture, unit, and integration tests before producing deployment artifacts.
- Build self-contained Linux EF migration bundles for IAM, HR, Shared, and Banking.
- Authenticate GitHub to Azure with short-lived OIDC credentials.
- Apply migrations before deploying application packages.
- Deploy API and UI to the explicitly selected target: Azure App Service, Azure Container Apps with ACR, or Azure Container Apps with GHCR.
- Avoid startup migrations, App Service publish-profile secrets, and long-lived Azure client secrets.

## Pipeline Shape

1. **Build and test**
   - Restore explicit projects so Linux CI does not require unsupported MAUI workloads.
   - Build and test in `Release` mode.
   - Publish API/UI artifacts.
   - Build one `efbundle` per bounded-context DbContext.
2. **Run database migrations**
   - Authenticate through `azure/login@v2` using the `production` GitHub environment.
   - Add a temporary firewall rule for the GitHub-hosted runner.
   - Execute each context bundle against its dedicated migrations history table.
   - Retry while an Azure SQL serverless database resumes.
   - Remove the temporary firewall rule even after failure.
3. **Deploy API and UI to the selected target**
  - Authenticate each deployment job independently through OIDC.
  - Deploy the previously tested packages with `azure/webapps-deploy@v3` for App Service, or update the Container Apps revisions for an ACA target.

## Security Decisions

- GitHub deployment identity and App Service runtime managed identity are separate principals.
- The GitHub federated credential is scoped to the `production` GitHub environment.
- SQL migration networking is explicit: `PublicRunner` for temporary `/32` firewall access or `PrivateRunner` for a VNet-connected self-hosted runner.
- Azure resource names and identity IDs are GitHub environment variables.
- The passwordless Azure SQL connection string is a GitHub environment secret.
- Deployment approvals can be enforced with required reviewers on the GitHub environment.
- A private-endpoint-only production environment should use a self-hosted runner inside the Azure virtual network rather than opening public network access.

## Completion Gate

This pipeline is complete only when:

- OIDC login succeeds without client secrets.
- All four migration bundles run successfully and retain separate history tables.
- The temporary SQL firewall rule is removed.
- API and UI deploy only to the explicitly selected target.
- Post-deployment health and smoke checks pass.
- Rollback and deployment-slot promotion are documented and tested.
