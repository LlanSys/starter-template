# Renaming The Template

Rename a freshly cloned repository before you do anything else in it: before the first run, first commit or first deployment.

```powershell
cd E:\Repos\InsurHub
.\scripts\rename-template.ps1 -NewName InsurHub -NamespacePrefix IH -DatabaseProvider PostgreSql -ResetGitHistory -WhatIf
.\scripts\rename-template.ps1 -NewName InsurHub -NamespacePrefix IH -DatabaseProvider PostgreSql -ResetGitHistory
git remote add origin <new-remote-url>
git push -u origin main
```

This produces project and namespace names such as `IH.Domain`, `IH.Application`, and `IH.Api`. `NewName` is the product identity. `NamespacePrefix` is the short technical prefix. It is required so the script never guesses an acronym incorrectly.

`-DatabaseProvider` is required:

- `SqlServer` or `PostgreSql` removes the other provider completely: contexts, migrations, tests, packages, infrastructure and CI branches.
- `Both` keeps the template's dual-provider setup.

The choice can't be changed later in the fork. See [database provider selection](../architecture/database-provider-selection.md), which also covers switching provider later.

`-ResetGitHistory` discards the template's history. It first checks that the working tree is clean and that `origin` points at the template. It then deletes `.git` before anything else, and after renaming creates a fresh repository on `main` with a single initial commit. It adds no remote, so connect the new one yourself. Because `.git` is deleted first, recover from a failed run by cloning again. Leave the switch out if you want to handle git yourself.

Use `-WhatIf` first to preview changes. The script updates product-name variants in text files, reads the current prefix from AGENTS.md, changes dotted technical prefixes and updates the declared root prefix, handles the known compact `STApi` local-data path, and renames matching files and folders. It deliberately does not replace every standalone `BT`, because `BT` is also valid unrelated data such as Bhutan's ISO country code. It skips Git metadata, generated build output, logs, `output`, `tmp`, `TestResults`, dependencies, and `ops/local/.env`.

After the script runs:

1. Review URLs, email sender values, tenant seed data, Azure resource names, and deployment variables.
2. Restore and build the solution.
3. Configure local user-secrets or deployed environment variables.
4. Start the application only after the new identity is final.

## Data Protection Boundary

`DataProtection:ApplicationName` participates in cryptographic purpose chains. The rename script changes it safely only when used before protected data exists. Once cookies, TOTP secrets, trusted-device tokens, or other protected payloads have been issued, do not rename it casually. A later change requires a deliberate compatibility migration or security-state reset.

## Azure Resources

The script updates repository configuration tokens; it does not rename live Azure resources. Supply actual Key Vault, Storage, App Service, SQL, and Service Bus names through deployment variables and environment configuration.

## Independent local configuration

Renaming assigns fresh UserSecretsId values to host projects and updates design-time references. Provision local secrets for the renamed clone; secret contents are not copied. Modern PowerShell uses UUIDv7; Windows PowerShell 5.1 uses a cryptographically random identifier because its runtime lacks that API. Rerunning with the identical product name and prefix leaves the existing identities intact. Run `scripts/test-rename-template.ps1` from PowerShell 7 to verify dry-run behavior, project paths, secret references, repeated runs, generated-file exclusions, database provider selection, and git history reset.

Allocate distinct host ports and Compose project/volume names before starting a clone alongside another application. Run scripts/check-local-endpoints.ps1 with -ComposeFiles pointing to its Compose file; see [workstation endpoint allocations](workstation-endpoints.md).
