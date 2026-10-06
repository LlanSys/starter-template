# StarterTemplate extraction checkpoint

StarterTemplate is an independent private repository exported from BaseTemplate. ORIGIN.md records the immutable source revision. The domain-neutral refactor plan owns the current execution order; inherited roadmap claims describe the source baseline, not certification of this starter.

## Local configuration

No source secrets are copied. API and Blazor have distinct, regenerated UserSecretsId values. DevelopmentSeed is disabled and administrator credentials and IamProvisioning:TemporaryPassword are blank; configure them through user-secrets before enabling bootstrap. The current employee-linked bootstrap is due for replacement in Phase 1. Do not enable it for a real account yet.

Development email uses LocalMailpit. Production provider adapters remain configurable. DataProtection:ApplicationName is StarterTemplate and must be stable after first use.

| Service | StarterTemplate host ports |
| --- | --- |
| API HTTP / HTTPS | 5257 / 7229 |
| Blazor HTTP / HTTPS | 5206 / 7149 |
| Container API / Blazor | 8180 / 8181 |
| SQL Server | 14335; database ST |
| Redis | 6382 |
| RabbitMQ / management | 5675 / 15675 |
| Mailpit SMTP / UI | 1030 / 8030 |
| Seq | 5344 |
| Azurite blob / queue / table | 10020 / 10021 / 10022 |

Container service ports remain unchanged. Compose project cg-startertemplate and startertemplate_* volumes are independent. Run scripts/check-local-endpoints.ps1 with the new Compose files before startup. These defaults require a current workstation conflict check; no services were started during extraction.

## Delivery boundary

GitHub secrets, repository variables, environment bindings and Azure OIDC identities are intentionally unconfigured. Azure deployment and provisioning require the exact repository variable AZURE_DEPLOYMENT_ENABLED=true. Use new environments starter-azure-prod and starter-non-azure-prod and new cloud targets. Infrastructure defaults use stapp/stprod; database ST. Inherited compiled Bicep JSON must be regenerated when infrastructure changes.

Banking/HR and their migration history remain until the subsequent removal phases. Fresh initial migrations for retained contexts are generated after the model settles; never apply inherited migrations to source databases. No deployment, migration, browser or provider certification has been performed in the starter.

The export also excludes tracked root workstation SQL/HTTP diagnostics and temporary updater/file-list artifacts. Their originals remain in BaseTemplate.

## Phase 0 verification

- API build passed: 105 warnings, 0 errors.
- Blazor Web build passed: 16 warnings, 0 errors.
- Architecture tests passed: 80/80. Restore reported inherited Roslyn NU1608 dependency constraints.
- JSON and PowerShell syntax checks passed; both host secret IDs are distinct and do not match the source.
- GitHub repository created with private visibility; remote verification and disposable rename checks follow before phase closure.
- Unit tests passed: 37/37. Provider integration tests, mobile build, browser/email smoke and runtime startup are pending future certification gates.
