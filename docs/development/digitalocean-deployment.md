# DigitalOcean Deployment Guide

This document outlines the resources, configuration variables, and GitHub Actions setup required to deploy StarterTemplate to DigitalOcean.

DigitalOcean is an excellent cloud provider for this stack. However, since they do not offer a native Managed RabbitMQ service or Managed SQL Server, there are a few architectural choices to make.

## Deployment Flow

The end-to-end deployment lifecycle for DigitalOcean looks like this:

`VS -> GitHub Repo -> PR -> Workflow (Docker Image -> GHCR -> DO App Platform)`

- **VS**: You push code changes to GitHub.
- **GitHub Repo / PR**: The code is merged into your deployment branch.
- **Workflow**: A GitHub Action (`non-azure-deploy.yml`) builds your API and Blazor containers.
- **GHCR**: The action pushes the compiled container images to the GitHub Container Registry.
- **DO App Platform**: The action connects to DigitalOcean via `doctl` and triggers your App Platform App to pull the latest images and redeploy.

---

## 1. DigitalOcean Portal Resources

To run this application in production on DigitalOcean, you will need to provision the following resources in your DigitalOcean account:

### 1.1 App Platform (Compute)
This will host both the backend API and the frontend Blazor WebAssembly/Server application.
*   **Type**: App Platform (using GitHub Container Registry (GHCR) images)
*   **Purpose**: Runs your API container and Blazor UI container. Handles TLS termination and scaling.
*   **Placeholder Value Needed**: `your-do-app-id` (The ID of the created app, used in GitHub actions).

### 1.2 Managed Database (PostgreSQL)
StarterTemplate supports both SQL Server and PostgreSQL out of the box via Entity Framework Core. Since DO offers Managed PostgreSQL, it is the recommended choice.
*   **Type**: Managed Databases -> PostgreSQL
*   **Purpose**: Primary transactional database.
*   **Placeholder Value Needed**: `postgres://[user]:[password]@[host]:[port]/[database]?sslmode=require`

*(Note: If you strongly prefer SQL Server, you must run it yourself on a DigitalOcean Droplet or use an external provider, as DO does not offer Managed SQL Server).*

### 1.3 Managed Redis
*   **Type**: Managed Databases -> Redis
*   **Purpose**: Distributed caching, session management, and Data Protection Key-Ring storage.
*   **Placeholder Value Needed**: `rediss://[user]:[password]@[host]:[port]`

### 1.4 DigitalOcean Spaces (S3-Compatible Object Storage)
*   **Type**: Spaces Object Storage
*   **Purpose**: Storing user profile pictures and other uploaded assets.
*   **Values Needed**:
    *   **Endpoint**: e.g., `nyc3.digitaloceanspaces.com`
    *   **Bucket Name**: e.g., `llancore-assets`
    *   **Access Key & Secret Key**: Generated in the DO API section under "Spaces Keys".

### 1.5 Messaging: RabbitMQ (Custom Droplet or External)
DigitalOcean **does not** offer a Managed RabbitMQ service. You have two options for the MassTransit message bus:
1.  **CloudAMQP**: A third-party managed RabbitMQ provider (easiest, highly recommended).
2.  **Droplet**: Provision a basic DO Droplet (VM) and install RabbitMQ using Docker.
*   **Values Needed**:
    *   **Host**: `amqps://[hostname]`
    *   **Username / Password**: Administrator credentials.

---

## 2. Configuration Settings (POCOs / JSON / Secrets)

Your application is controlled via `appsettings.json` and environment variables. On DigitalOcean App Platform, you set these under the "Environment Variables" section for the API and Blazor components.

**Never commit production secrets to Git.** Use App Platform's encrypted environment variables.

### Environment Variables Needed in DO App Platform:

```text
ASPNETCORE_ENVIRONMENT=Production

# Database Configuration (PostgreSQL Recommended on DO)
DatabaseSettings__Provider=PostgreSql
ConnectionStrings__DefaultConnection=postgres://[user]:[password]@[host]:[port]/[database]?sslmode=require

# JWT Secrets
JwtSettings__SecretKey=<Generate-A-Strong-256Bit-Random-String-Here>

# Base URLs
BackendApi__BaseUrl=https://<your-blazor-domain>/api/  # If API and Blazor share a domain
AllowedOrigins__0=https://<your-blazor-domain>

# Redis Configuration
CacheSettings__Provider=Redis
CacheSettings__Redis__ConnectionString=rediss://[user]:[password]@[host]:[port]

# Data Protection (Store keys in Redis so scaling works)
DataProtection__UseExternalKeyStore=true
DataProtection__RedisKeyRingConnectionString=rediss://[user]:[password]@[host]:[port]
DataProtection__RedisKeyRingKey=DataProtection-Keys

# DO Spaces (S3 Provider for Profile Pictures)
ProfileImageStorage__Provider=S3
ProfileImageStorage__S3__ServiceUrl=https://nyc3.digitaloceanspaces.com
ProfileImageStorage__S3__AccessKey=<Your-DO-Spaces-Access-Key>
ProfileImageStorage__S3__SecretKey=<Your-DO-Spaces-Secret-Key>
ProfileImageStorage__S3__BucketName=llancore-assets
ProfileImageStorage__S3__PublicBaseUrl=https://llancore-assets.nyc3.cdn.digitaloceanspaces.com

# RabbitMQ / MassTransit
Messaging__Transport=RabbitMq
Messaging__RabbitMq__Host=<CloudAMQP-or-Droplet-Host>
Messaging__RabbitMq__Username=<RabbitMq-Username>
Messaging__RabbitMq__Password=<RabbitMq-Password>

# Email & SMS (e.g., SendGrid & Twilio)
EmailSettings__Provider=SendGrid
EmailSettings__SendGrid__ApiKey=<SendGrid-Key>
PasswordRecovery__Mode=EmailOtp
PasswordRecovery__ResetPath=/iam/reset-password
```

### Local Development Alignment

In your local `secrets.json` (managed via `dotnet user-secrets`), you can mimic this setup if you want to test against DO resources, but generally, local dev should use the provided `docker-compose.yml` (Local SQL Server, Local Redis, Local RabbitMQ, Local Azurite).

To ensure the POCOs map correctly, ensure your `appsettings.json` blocks correspond exactly to the keys above using double-underscores (`__`) to denote JSON hierarchy.

---

## 3. GitHub Repo Variables & Secrets

To enable Continuous Deployment (CD) from GitHub Actions to your DigitalOcean App Platform, you need to configure the following in your GitHub Repository (`Settings` -> `Secrets and variables` -> `Actions`):

### GitHub Actions Secrets
1.  `DIGITALOCEAN_ACCESS_TOKEN`: A personal access token generated from your DO API Dashboard. This allows GitHub Actions to use `doctl` to deploy your application.
2.  `CR_PAT` (optional): If your GitHub Container Registry is strictly private, you may need a PAT to push/pull Docker images.

### GitHub Actions Variables
1.  `DIGITALOCEAN_APP_ID`: The unique UUID of your created App in the DigitalOcean App Platform. This tells the deploy script which app to update.

### Deployment Workflow
The deployment pipeline (`.github/workflows/non-azure-deploy.yml`) is already configured. It will:
1. Build the Docker images for `ST.Api` and `ST.UI.Blazor`.
2. Push them to the GitHub Container Registry (GHCR).
3. Connect to DigitalOcean via `doctl` and trigger a re-deployment of your App Platform app using the newly pushed images.
