# Local operator acceptance results

> Follow-up: the catalogue/reset gap (ISO-03) is closed for the later local working tree. See [organization-settings verification](organization-settings-verification-2026-11-15.md). This report and its archived pack retain the original run outcomes; SSO-01 remains deferred to Allan Run.

Started 14 November 2026; completed checks recorded 15 November 2026 (Africa/Nairobi). Work used the existing `E:\Repos\StarterTemplate` checkout and its local Docker deployment. No new worktree, database reset, owner password change or owner MFA reset was used.

## Operational baseline

Nine generated platform operators are active, email-verified and authenticator-enrolled: `qa.staff-admin`, `qa.onboarding`, `qa.collector`, `qa.reviewer`, `qa.support`, `qa.auditor`, `qa.retention`, `qa.lifecycle` and `qa.tasks`. Their saved explicit grants match the vendor profiles and have access-change history. Profiles are selection presets; ordinary role names do not confer platform authority.

| Customer workspace | State | Administrator | Human accounts |
| --- | --- | --- | --- |
| qa-alder-01 / Alderfield QA Services | Active; KYC approved; setup complete | qa.shared.admin@example.test | Administrator, coordinator, reader; all active, verified and MFA-enrolled |
| qa-brook-02 / Brookmere QA Supplies | Active; KYC approved; setup complete | qa.shared.admin@example.test | Administrator, coordinator, reader; all active, verified and MFA-enrolled |

Both use the same pooled stamp and MaxUsers=3. Their matching administrator email resolves to separate IAM accounts, credentials and authenticator enrollments. Existing unrelated local workspaces were preserved. All companies and evidence in these runs are synthetic.

## What was exercised

Normal product APIs and password/MFA-issued JWT sessions exercised registration, mandatory KYC gates, typed platform/client uploads, actual ClamAV clearance, original-byte hash checks, evidence review, a correction cycle, encrypted-file rejection, explicit final approval, automatic pooled readiness, Mailpit welcome delivery, invitation resend/old-link rejection, restricted administrator setup, MFA completion and fresh sign-in. Each customer administrator created custom coordinator/reader roles and two members; fourth-human admission was denied.

Further live checks passed: foreign IDs/selectors and reserved-permission delegation were denied; tenant settings reads/writes/deletion stayed isolated; customer-approved support enforced requested scope/read-only status and live revocation; suspension blocked only A while B worked; reactivation preserved KYC and required fresh MFA; reasoned case holds were placed/released without changing global policies; role revocation and account deactivation invalidated effective access. Support proxies are absent from ordinary people lists and ordinary IAM mutations/invitations reject them. Temporary support grants and holds are revoked/released, and both coordinators' intended grants are restored.

Task assignment was exercised while each case awaited review: an ineligible auditor was rejected, an eligible reviewer received the task, and it left the unassigned queue. Source completion resolved the work.

## Browser evidence versus API evidence

The browser used normal password/authenticator entry for generated accounts. It observed the staff administrator, collector and customer coordinator separately. The collector could open KYC collection/review metadata but could not enter staff administration. The coordinator saw only permitted administration options and read-only IAM controls. During an active Brook user listing, removing users.view made the next protected request clear user results and remove Users/User Devices tabs while Roles remained. Baseline grants were restored and reloaded. On the final staff UI, a partially entered Add Staff form retained its name and collector preset for 353 seconds across routine session refresh; the probe was cancelled without saving.

The full eight-profile allowed/denied matrix is real local API evidence, not a claim that every profile received a separate complete browser walkthrough. Fresh Google/Microsoft sign-in plus app MFA was explicitly deferred by Allan to the independent Allan Run. Provider connection alone does not satisfy that check.

## Verification

- 249 unit tests, 74 architecture tests and 107 integration tests passed; no skips in the completed integration run, including real local RabbitMQ outbox delivery.
- Focused source formatting passed, including 19 formatter preservation/idempotence fixtures. Git whitespace verification passed.
- API and Blazor Linux images built and run successfully. Analyzer warnings remain; this is not a zero-warning build claim.
- Local ControlPlane, IAM and Shared migration status: zero pending. SQL Server/PostgreSQL staff/permission upgrade cases are included in integration coverage.
- A copy-only SQL backup is retained at `/var/opt/mssql/backup/ST-operator-baselines-20261115.bak` in the SQL container volume. CHECKSUM and RESTORE VERIFYONLY succeeded. This is backup integrity verification, not a restore/rollback rehearsal; protected storage and key rings require their own preserved recovery plan.

The detailed evidence lives in `output/pdf/operator-acceptance`: qa-results.csv, live-run-evidence.json, platform-profile-evidence.json, final-baseline-state.json, verification-summary.json and sanitized screenshots. The source-fix history is [the defect register](operator-acceptance-defects-2026-11-14.md).

## Remaining boundaries

The agent result sheet has 40 PASS, one BLOCKED and one NOT RUN. The blocked case is vendor-catalogued organization settings with a business consumer and reset-to-default semantics: generic key/value storage isolation passes separately, and must not be represented as that unimplemented capability. The unrun case is fresh Google/Microsoft sign-in plus app MFA, deferred by Allan.

Cloud deployment, dedicated-runtime certification, production load/availability, restore/rollback rehearsal and cloud evidence/erasure-ledger recovery remain separate sign-off. Public Start Trial intake, a trial clock/billing journey, paperless KYC forms and automated document authenticity verification are separate capabilities. Antivirus clearance is not identity/company verification. This local exercise is not blanket enterprise production certification.

## Allan's independent run

Use `output/pdf/startertemplate-allan-run.zip`. It has separate `qa-alder-allan-01` and `qa-brook-allan-02` identifiers, twelve synthetic PDFs and a blank results sheet. Those two workspaces have not been created. Reuse the platform operators after fresh sign-in, then perform the customer creation/invitation steps yourself.

See [local QA account handoff](local-qa-account-handoff.md). The generated credentials and authenticator material are Windows-user encrypted outside the repository. No credentials, seeds or invitation/submission tokens belong in the packs. Current source and required untracked feature files remain uncommitted; publish the tested tree before treating a downstream clone from main as this baseline.

Current QA logins and company addresses have since changed through the product; historical tables above retain the original shared-email isolation scenario. See [the current identity/routing acceptance](frontend-host-routing-verification-2026-11-15.md) and [credential handoff](local-qa-account-handoff.md).
