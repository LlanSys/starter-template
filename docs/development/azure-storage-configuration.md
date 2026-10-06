# Azure Storage And IAM Configuration

This guide explains how StarterTemplate uses Azure Storage, Key Vault, managed identity, and local user-secrets for platform storage and IAM smoke testing.

## Why These Settings Exist

StarterTemplate uses Azure platform services for these concerns:

- **ASP.NET Core Data Protection keys:** protects auth cookies, antiforgery payloads, and other secure framework payloads. In production or multi-instance hosting, keys must survive restarts and be shared by all instances.
- **Key Vault key protection:** encrypts the persisted Data Protection key ring so a storage-account read alone is not enough to decrypt protected application data.
- **Profile images:** stores uploaded account avatars outside the application filesystem. The blobs remain private and are served through an authorized API endpoint.
- **Managed identity:** lets Azure App Service access Storage and Key Vault without storing connection strings or secrets in App Service settings.

Keep Data Protection keys and profile images in separate blob containers.

`DataProtection:ApplicationName` is the application discriminator embedded in Data Protection purpose chains. It must remain stable across local runs, deployments, and slots whenever existing cookies, TOTP secrets, or protected tokens must remain readable. The unrenamed template uses `StarterTemplate`. Rename it before the cloned application first runs; changing it after protected data exists is a security-data migration, not a branding change.

## Recommended Containers

Create these blob containers in the storage account:

- `dataprotection-keys`
- `profile-images`

Recommended access level:

- `dataprotection-keys`: private.
- `profile-images`: private by default.

`profile-images` should remain private. The database stores the internal storage URI, but API responses expose `/api/v1/iam/users/me/profile-picture/content`. The browser never receives Azure credentials, account keys, SAS tokens, or direct private blob URLs.

## Azure Portal Setup

### 1. Storage Account Containers

In Azure Portal:

1. Open the storage account.
2. Go to **Data storage** -> **Containers**.
3. Create `dataprotection-keys`.
4. Create `profile-images`.
5. Keep both containers **Private**.

### 2. Key Vault Key

In Azure Portal:

1. Open the Key Vault.
2. Go to **Objects** -> **Keys**.
3. Click **Generate/Import**.
4. Use a name such as `bt-dataprotection`.
5. Use `RSA`, preferably `2048` or higher.
6. Create the key.
7. Open the created key, then open the current version.
8. Copy **Key Identifier**.

The copied value becomes `DataProtection__KeyVaultKeyIdentifier`.

### 3. App Service Managed Identity

In Azure Portal:

1. Open the App Service.
2. Go to **Settings** -> **Identity**.
3. Under **System assigned**, set **Status** to **On**.
4. Save.

This creates an Azure identity for the app. We grant permissions to this identity instead of storing Azure passwords in configuration.

### 4. Storage RBAC

In Azure Portal:

1. Open the storage account.
2. Go to **Access Control (IAM)**.
3. Click **Add** -> **Add role assignment**.
4. Select role `Storage Blob Data Contributor`.
5. Assign access to **Managed identity**.
6. Select the App Service managed identity.
7. Review and assign.

For local development with your Azure login instead of a connection string, assign the same `Storage Blob Data Contributor` role to your own Azure user account.

### 5. Key Vault RBAC

In Azure Portal:

1. Open the Key Vault.
2. Go to **Access Control (IAM)**.
3. Click **Add** -> **Add role assignment**.
4. Select role `Key Vault Crypto User`.
5. Assign access to **Managed identity**.
6. Select the App Service managed identity.
7. Review and assign.

To create/view keys yourself in the portal, your own Azure user also needs a data-plane role such as `Key Vault Crypto Officer` or `Key Vault Administrator`.

## Local User-Secrets

User-secrets are for local development only. They override `appsettings.Development.json`.

### Option A: Local Development With Storage Connection String

Use this if your local Visual Studio/Azure CLI identity is not configured for Azure RBAC yet.

```powershell
dotnet user-secrets set "ProfileImageStorage:Provider" "AzureBlob" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "ProfileImageStorage:AzureBlob:ConnectionString" "<storage-connection-string>" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "ProfileImageStorage:AzureBlob:ContainerName" "profile-images" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "ProfileImageStorage:AzureBlob:BlobPrefix" "profile-images" --project src\Backend\Api\ST.Api\ST.Api.csproj
```

`ConnectionString` comes from Azure Portal -> Storage account -> **Security + networking** -> **Access keys** -> **Show** -> copy the connection string. Use `key1` by default; keep `key2` available for key rotation.

### Option B: Local Development With Your Azure Identity

Use this if your signed-in Visual Studio/Azure CLI account has `Storage Blob Data Contributor`.

```powershell
dotnet user-secrets set "ProfileImageStorage:Provider" "AzureBlob" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "ProfileImageStorage:AzureBlob:ContainerUri" "https://<storage-account>.blob.core.windows.net/profile-images" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "ProfileImageStorage:AzureBlob:BlobPrefix" "profile-images" --project src\Backend\Api\ST.Api\ST.Api.csproj
```

### Data Protection With Azure Blob And Key Vault

```powershell
dotnet user-secrets set "DataProtection:KeyEncryptionMode" "KeyVault" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "DataProtection:BlobKeyUri" "https://<storage-account>.blob.core.windows.net/dataprotection-keys/keyring.xml" --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "DataProtection:KeyVaultKeyIdentifier" "https://<key-vault-name>.vault.azure.net/keys/<key-name>/<key-version>" --project src\Backend\Api\ST.Api\ST.Api.csproj
```

`DataProtection:BlobKeyUri` is constructed manually. The `keyring.xml` blob does not need to exist first; the app creates it.

`DataProtection:KeyVaultKeyIdentifier` is copied from the Key Vault key version.

`DataProtection:KeyEncryptionMode` can be:

- `Auto`: prefer Key Vault when `KeyVaultKeyIdentifier` is configured; otherwise use certificate when `CertificateThumbprint` is configured; otherwise persist without extra key encryption.
- `KeyVault`: require `KeyVaultKeyIdentifier` and fail fast if it is missing.
- `Certificate`: require `CertificateThumbprint` and fail fast if the certificate is not loaded with a private key.
- `None`: persist keys without an additional Key Vault key or certificate wrapper. Use only for local-only development.

If Key Vault is not ready yet, Blob key persistence can be tested without `DataProtection:KeyVaultKeyIdentifier`, but production should encrypt keys with Key Vault.

### Current Local Secrets Check

To see what local secrets are active:

```powershell
dotnet user-secrets list --project src\Backend\Api\ST.Api\ST.Api.csproj
```

If `ProfileImageStorage:Provider=AzureBlob` appears there, uploads will go to Azure even though `appsettings.Development.json` says `Local`.

## Azure App Service Settings

Use App Service configuration or Key Vault references for the same keys:

```text
ProfileImageStorage__Provider=AzureBlob
ProfileImageStorage__AzureBlob__ContainerUri=https://<storage-account>.blob.core.windows.net/profile-images
ProfileImageStorage__AzureBlob__BlobPrefix=profile-images
DataProtection__BlobKeyUri=https://<storage-account>.blob.core.windows.net/dataprotection-keys/keyring.xml
DataProtection__KeyEncryptionMode=KeyVault
DataProtection__ApplicationName=StarterTemplate
DataProtection__KeyVaultKeyIdentifier=https://<key-vault-name>.vault.azure.net/keys/<key-name>/<key-version>
```

Prefer `ContainerUri` plus managed identity in Azure. Use `ConnectionString` only for local development or controlled non-managed-identity environments.

Prefer Key Vault key encryption for Data Protection in Azure. `DataProtection__CertificateThumbprint` and `WEBSITE_LOAD_CERTIFICATES` are only needed when deliberately using `DataProtection__KeyEncryptionMode=Certificate` as a fallback. Do not configure both as active choices unless the mode is `Auto`, where the application prefers Key Vault.

### Where Values Come From

- `ProfileImageStorage__AzureBlob__ContainerUri`: Azure Portal -> Storage account -> Data storage -> Containers -> `profile-images` -> Properties -> URL. It looks like `https://<storage-account>.blob.core.windows.net/profile-images`.
- `ProfileImageStorage:AzureBlob:ConnectionString`: Azure Portal -> Storage account -> Security + networking -> Access keys -> Show -> copy the key connection string. Use this only for local/dev fallback.
- `DataProtection__BlobKeyUri`: construct this from the storage account and container, for example `https://<storage-account>.blob.core.windows.net/dataprotection-keys/keyring.xml`. The `keyring.xml` blob does not need to exist first; the app creates it.
- `DataProtection__KeyVaultKeyIdentifier`: Azure Portal -> Key Vault -> Objects -> Keys -> select/create key -> current version -> copy Key Identifier. It looks like `https://<vault-name>.vault.azure.net/keys/<key-name>/<key-version>`.

## Where Profile Image URLs Are Saved

Profile image storage URI is saved on the IAM user:

```sql
select Id, Email, ProfilePictureUrl
from AspNetUsers
where Email = 'aamodhiambo@gmail.com';
```

It is not saved in `AppUserProfiles`. `AppUserProfiles` is for additional profile/contact metadata.

For Azure storage, the blob appears in the `profile-images` container. With the default `BlobPrefix=profile-images`, the blob path is:

```text
profile-images/<app-user-id>/<random-file-name>
```

Because the container is also named `profile-images`, the portal can look like `profile-images/profile-images/...`. That is expected with the current prefix.

The UI should receive this API URL:

```text
/api/v1/iam/users/me/profile-picture/content
```

To confirm in the browser:

1. Open DevTools.
2. Inspect the profile avatar image or open the Network tab.
3. Filter for `profile-picture`.
4. Confirm the request is to `/api/v1/iam/users/me/profile-picture/content`, not a direct `blob.core.windows.net` URL.

## Required Azure RBAC

For the API managed identity:

- Storage Blob Data Contributor on the storage account or target containers.
- Key Vault Crypto User on the Key Vault key used to protect Data Protection keys.

## Smoke Test

### Non-Admin Flow

Seeded non-admin employee users are intentionally inactive until an admin grants system access.

Seeded employees:

- `allan.alex0803@gmail.com`
- `omitolaura469@gmail.com`

Flow:

1. Sign in as admin.
2. Go to **Admin Center** -> **Employees**.
3. Use **Grant System Access** for Allan or Laura.
4. Assign a role such as `Employee`.
5. Confirm the activation email is sent.
6. Sign out.
7. Sign in as the non-admin user with `IamProvisioning:TemporaryPassword`.

To confirm or change the temporary password:

```powershell
dotnet user-secrets list --project src\Backend\Api\ST.Api\ST.Api.csproj
dotnet user-secrets set "IamProvisioning:TemporaryPassword" "<new-temp-password>" --project src\Backend\Api\ST.Api\ST.Api.csproj
```

### Admin Flow

Seeded admin:

```text
aamodhiambo@gmail.com
```

The seeded admin password comes from `DevelopmentSeed:AdminPassword`.

Current development default:

```text
Admin@12345
```

Admins are in the MFA-required role list. Enable MFA for admin before testing admin-only actions such as profile upload if `SecuritySettings:Mfa:EnforceEnrollment=true`.

### Profile Image Flow

1. Ensure the user is allowed past MFA enrollment.
2. Upload a profile image from `/profile`.
3. Confirm `AspNetUsers.ProfilePictureUrl` is populated.
4. Confirm the blob exists in the `profile-images` container.
5. Confirm the UI image source is `/api/v1/iam/users/me/profile-picture/content`.
6. Restart the API twice and confirm login/session/MFA flows still work when `DataProtection:BlobKeyUri` is configured.
