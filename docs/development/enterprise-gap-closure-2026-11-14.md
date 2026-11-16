# Local enterprise gap closure — 2026-11-14

Scope: tenant-consistent ordinary account creation, concurrent MaxUsers admission/restoration, and KYC original-file safety. Preserve StarterTemplate IAM, mandatory KYC, tenant/stamp ownership, explicit platform permissions, SSO/MFA, invitation onboarding and retention gates. This is verification of the local working revision based on `9819e9d`; the changes are uncommitted, not a published GitHub template revision.

## Implemented

- Ordinary account creation and invitation onboarding share tenant-scoped identity semantics. Deleted identities stay reserved in their workspace. A unique tenant/normalized-email index complements the existing username index; upgrades reject legacy duplicates without merging/deleting identities.
- Identity storage serializes admission/restoration/key changes with transaction-owned tenant locks on SQL Server and PostgreSQL. Non-deleted humans include inactive/setup accounts. Trusted proxy kinds are persisted explicitly and narrowly backfilled from paired original actor/proxy markers. Missing central tenant limits fail closed.
- New and existing originals remain private and quarantined until real ClamAV scanning clears their original checksum. Persisted leases, revisions, deadlines, definition versions/freshness, bounded retries and reviewer rescan requests prevent stale results or provider outages from granting access. Encrypted, unsafe and inconclusive files remain unavailable.
- Downloads verify original length/checksum again; changed/missing originals become visibly Blocked and can be rescanned. Accepting documents requires Clean. Initial KYC approval rejects uncleared evidence under consideration. Requesting replacement remains possible without downloading unsafe files.
- Dedicated customer runtimes cannot lease the central KYC scan queue. Retention continues to own physical purge/metadata erasure; scanner ledger handling cannot make pending deletion work disappear.
- Rate-limit policies now consume validated options rather than an early configuration copy. Production defaults remain unchanged. The synthetic acceptance matrix has its own upload budget.
- Integration test collection concurrency is capped at one. An initial broad run overloaded Docker and made the engine unreachable; Docker Desktop was recovered and existing services restarted without deleting volumes or resetting databases. Subsequent bounded runs stayed stable.

## Verification

| Check | Result |
| --- | --- |
| Unit suite, final revision | 232 passed |
| Architecture suite, final revision | 74 passed |
| Integration regressions with real RabbitMQ delivery | 92 passed, zero skipped; KYC scanner case excluded here because separately certified below |
| Real KYC API/scanner acceptance | 1 passed using disposable SQL Server, private Azurite and ClamAV 1.5.4 with current definitions |
| Total distinct integration checks | 93 passed |
| Focused historical upgrades, quota/lease races and retention | SQL Server and PostgreSQL passed; includes safe duplicate-upgrade failure and preservation/recovery |
| Source formatter | Focused passes and all 19 preservation/idempotence fixtures passed |
| Dependency policy | 13 projects, 87 central versions; .NET 10 policy passed |
| Disposable downstream copy | 1,662 source files verified, including needed untracked files; ignored local secrets/build outputs excluded |
| Renamed downstream | EnterpriseProbe / EP; restore, API build, Blazor build and all 74 architecture checks passed |
| Clone isolation | Four distinct original/clone API/UI secret-store identities; no local .env copied; clone never started |

The source export manifest SHA-256 was `2160b088fbd6522254f8608530912e4cced2c77d9e4e2916da9f2937c5d6acd6`. Documentation of final outcomes was added after export verification. The disposable source copy and temporary PDF-fixture tooling were removed after checks.

KYC acceptance covers private platform/client upload, authenticated permission gates, cross-workspace denial, hashed/rotating/revocable submission links, pending download/acceptance denial, clean download, actual EICAR-test PDF rejection, encrypted PDF blocking, forced dependency outage and real-scanner recovery, stale rescan rejection, integrity-change download denial, type-specific deduplication, final approval gating and upload/revocation races. Antivirus proves file safety under its signatures, not document authenticity.

## Existing local database and apps

The native API/UI ports had no listeners before maintenance. Only StarterTemplate's Docker API/UI were paused. The existing `ST` database at `localhost,14335` received a verified copy-only backup inside its existing SQL volume:

`/var/opt/mssql/backup/ST-enterprise-before-20261114-01a19000.bak`

The guarded local upgrade applied `20261113220648_QuarantineKycEvidence` and `20261113220638_EnforceTenantAccountAdmission`. ControlPlane, IAM and Shared all report zero pending migrations. No database reset or account/credential reset was used. Existing non-erased evidence is queued for scanning.

- [x] Updated Linux API and UI images built sequentially and started with the private scanner.
- [x] API `/health` and `/health/ready`: HTTP 200; UI `/iam/sign-in`: HTTP 200.
- [x] Anonymous KYC document listing and GraphQL operations: HTTP 401.
- [x] All three context histories: zero pending migrations; endpoint audit passed 76 distinct allocations and nine secret stores.
- [x] Scanner healthy with ClamAV 1.5.4, definition version 28182 (2026-11-13 UTC); zero KYC safety batch failure log entries.

The existing local database had zero KYC document rows, so there was no live legacy scan backlog. Actual clean/unsafe/encrypted and outage/recovery processing was proved using the disposable acceptance database/storage. Existing account classification after upgrade was two Human accounts and two DemoProxy accounts. No account, credential or key-ring reset was used.

## Remaining certification boundaries

Cloud identity/network/storage/scanner deployment, backup/restore rehearsal, production load/availability testing and separate dedicated-runtime deployment certification remain outside this local pass. Live provider sign-in browser/MFA ceremonies were not rerun for these changes. OCR, registry checks and automated document authenticity decisions, paperless forms and public trial/billing journeys are separate capabilities; they are not supplied by antivirus scanning.

Use [the configuration and upgrade runbook](account-admission-and-kyc-safety.md) and [downstream checklist](downstream-configuration-checklist.md) for application-specific configuration. Publish the tested tree before treating GitHub main as this baseline.
