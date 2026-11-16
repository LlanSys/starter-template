# Organization settings and workspace verification - 15 November 2026

## Result

The earlier ISO-03 organization-settings capability gap is closed for the current local working tree: a vendor catalogue, implemented presentation consumers, permission-protected save/reset, atomic tenant-scoped history and effective defaults are available. The previous operator pack records its earlier snapshot and is not rewritten. Fresh Google/Microsoft sign-in plus application MFA (SSO-01) remains deferred to Allan Run by Allan's instruction.

Implementation details and defaults: [organization settings](organization-settings.md). Independent checklist: [organization settings acceptance](organization-settings-acceptance.md).

## Executed checks

| Check | Result |
| --- | --- |
| Unit suite | 252 passed, zero failed/skipped; includes actual formatter source, UTC/TimeProvider, Nairobi, DST, DateOnly and regional formatting |
| Architecture suite | 74 passed, zero failed/skipped; includes explicit authenticated-only presentation projection exception |
| Broad integration suite | 110 passed, two new provider-fixture setup failures and one opt-in RabbitMQ skip |
| Corrected focused integration run | Eight passed, zero failed/skipped: both providers, four catalogue API cases, real JWT cache isolation and actual RabbitMQ transport |
| Distinct integration cases with passing evidence across those runs | All 113; the two failures were the isolated test host's missing AddLogging registration and passed after it was corrected |
| API/Blazor builds | Passed, including Linux container builds; analyzer warnings remain |
| Focused source formatting | Passed, including 19 formatter preservation/idempotence fixtures |
| Local endpoint audit | Passed: 76 allocations, nine independent secret stores |
| Local database | Additive SQL Server history migration applied; all three contexts have zero pending migrations |

Evidence: tests/ST.Tests.Unit/TestResults/organization-settings-unit.trx, tests/ST.Tests.Architecture/TestResults/organization-settings-architecture.trx, tests/ST.Tests.Integration/TestResults/organization-settings-complete-integration.trx and organization-settings-final-integration.trx. The final UI-only header/profile changes were built, formatted and observed in the browser after backend testing.

## Real local activity

Used the existing Docker deployment, accounts and customer workspaces. No new worktree, account, tenant, password, MFA seed or secret was required.

- Sign-in resolved qa-brook-02 and qa-alder-01 to their actual organization names and synchronized the workspace URL. A deliberately unavailable identifier showed a rejection while keeping credential/provider controls hidden. Change Workspace removed the unsent username draft.
- Signed in as the existing Alder administrator through password and authenticator MFA. The settings catalogue showed all five supplied defaults, without requiring stored overrides.
- Saved Africa/Nairobi through the real searchable dialog. The preview changed from UTC to +03:00, and history recorded the stable actor and change time.
- Reset through More > Reset To Default. The row returned to UTC/Default, and both original change and reset remained visible in history. All five supported Alder settings finished on defaults.
- Independently signed in through real JWT/MFA sessions as the Brook administrator and reader. Brook retained UTC/en-GB/dd MMM yyyy/24/25 while Alder had an override. The reader received the presentation projection, but catalogue reads and updates returned 403. See output/organization-settings/live-settings-checks.json.
- Final browser reload verified the organization name in the main header and Profile, and a real last-sign-in timestamp consumer. Profile's time-zone label now follows the display preference rather than hardcoding UTC.
- API health and UI sign-in returned HTTP 200. API and Blazor were rebuilt/recreated without removing volumes or changing provider credentials. Temporary plaintext credential/code transfer files were removed and two helper-cached bearer tokens were cleared. QA accounts and MFA enrollments remain available.

Screenshots: output/organization-settings/workspace-sign-in.jpg and organization-settings.jpg. The observed browser used its default viewport; this is not a full device/accessibility/browser matrix.

Before migration, ST-organization-settings-20261115.bak was created in the SQL container's /var/opt/mssql/backup directory with COPY_ONLY/CHECKSUM and passed RESTORE VERIFYONLY. That checks backup integrity, not a restore rehearsal or a blob/key-ring backup.

## Remaining boundaries

The current tree is uncommitted; no push, cloud deployment or new downstream-clone certification was performed. Dedicated-stamp/cloud/recovery sign-off remains separate. Fresh Google/Microsoft provider sign-in plus MFA is still Allan's deferred check. Regional formatting does not mean translated interface resources, currency conversion, personal preference overrides or automatic tenant-domain provisioning.
