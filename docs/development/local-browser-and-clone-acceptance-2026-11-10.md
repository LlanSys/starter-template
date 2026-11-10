# Local browser and downstream acceptance — 2026-11-10–11

Local core checks started on 10 November and completed on 11 November 2026 (Africa/Nairobi). Provider-account and cloud sign-off remain separate below.

## Scope and identity

The starting published baseline was `eb8f225d4a72a09fecc17170acbf2d1ad3fb1865` from `LlanSys/starter-template/main`. This pass used disposable local identities and databases. The existing StarterTemplate application database, accounts, provider credentials and infrastructure volumes were preserved. Azure execution remained disabled.

The Docker walkthrough used API `http://localhost:18590` and Blazor `http://localhost:18591`, a separate `ST_DockerAcceptance_20261110` database, and labeled disposable app containers. The API used the existing published-baseline image; the corrected Blazor code was published and layered on the existing runtime image. The normal Dockerfile build encountered a slow unrelated SDK/base-image download; this local pass did not certify an updated base-image version. Native clone verification used API `18580`, UI `18581` and a separate `NA_Acceptance_20261110` database. Local SQL Server and Mailpit services were deliberately shared; application databases, key rings and secret-store identities were independent.

Messaging was disabled in the disposable environments. This pass does not add messaging transport certification. Earlier provider/migration/outbox evidence remains recorded separately in the completion checklist.

## Completed browser scenarios

| Scenario | Observed result |
| --- | --- |
| Vendor credential sign-in and authenticator challenge | Normal password sign-in, TOTP challenge, Dashboard and Control Panel succeeded. |
| Two customers sharing an email address | `customer-a` and `customer-b` signed in independently through the browser with their own TOTP challenges. Account names and authorization remained customer-specific. |
| Tenant-owned IAM data | Created `Acceptance Reader A` through customer A's UI. A listed the role; B listed only its own Tenant Administrator role. |
| Platform route denial | A customer visiting `/control-panel` receives Page unavailable. The Dashboard link works and the circuit remains usable. |
| Customer-scoped support request | Vendor requested bounded read-only `menus.view` access for A. A saw/reviewed the request; B had no request. |
| Support entry and ordinary exit | Customer approval, visible Support/Read-only context, view-only navigation listing and End support and return succeeded. The platform selection/identity was restored and A's separate session remained normal. |
| Customer revocation | A revoked a separate active support grant. The support tab's next request returned to sign-in, offering Return to platform. Recovery restored the platform account. |
| Suspension | Vendor suspended A while A was signed in. A lost access and a new sign-in attempt reported Workspace is not available. B's existing session continued working. |
| Reactivation | A stayed at sign-in after reactivation. Fresh password and TOTP verification restored access. B remained unaffected. |
| Demo regression | Demo entry, hard reload and Return to platform succeeded with the correct workspace context. |
| UI replacement/key continuity | Replacing the disposable UI while preserving its key ring retained ordinary protected browser sessions. |

Synthetic customer fixtures were prepared through real API KYC approval, durable Mailpit invitation delivery, explicit invitation acceptance, normal Identity password login, TOTP enrollment/challenge and administrator completion. These preparation steps used normal HTTP APIs; they were not all repeated as browser form submissions. No test authentication handler, forged permission token or authentication bypass was installed in the Docker runtime. The browser exercised the resulting real accounts and MFA/session boundaries.

## Corroborating Docker HTTP checks

All 16 checks passed through normal signed sessions against the disposable Docker API: A-only role visibility, B's foreign-role permission denial, tenant Control Plane denial, signed-tenant/selector mismatch denial, wrong-customer support approval denial, read-only/no-platform support identity, support write denial, revoked access and refresh denial, suspension access/refresh denial, B continuity, old access/refresh denial after reactivation, and fresh password/TOTP sign-in after reactivation.

## Defects found and corrected

1. Support/Demo entry changed tokens without changing the protected workspace selection. The signed tenant and request selector therefore disagreed and support entry returned to sign-in. Entry now saves the platform selection with its session, selects the target workspace, and stores the scoped tokens. Ordinary exit and sign-in recovery restore the exact platform selection/session. Existing backups without the new selection field remain readable.
2. The unauthorized-route fragment added MainLayout inside AuthorizeRouteView's own default layout. The duplicate MudBlazor provider crashed the denied-route circuit. The denial content now uses the single framework-provided layout. Direct-route denial and recovery were repeated in the browser after the fix.

## Fresh GitHub clone verification

- Cloned the published GitHub revision rather than copying the working directory or using a Git worktree.
- Ran the shipped rename script as `NeutralAcceptance`, namespace prefix `NA`, before first run.
- Confirmed two fresh, independent host UserSecretsId values and absence of the source application's ignored `.env`.
- Allocated native, Docker and infrastructure ports independently. The combined endpoint audit passed; Docker host ports were distinct from native ports.
- API and Blazor builds passed. All 184 renamed unit tests and 74 renamed architecture tests passed, including provider model checks. Existing analyzer/package warnings remain.
- Applied all fresh SQL Server ControlPlane, IAM and Shared migrations, including Quartz compatibility, to the clone's own database.
- Started both renamed native hosts, received HTTP 200 health/sign-in responses, performed a real credential login, and observed the renamed UI plus mandatory MFA-enrollment page.
- The workstation's local EF command resolver reported that tools were not restored even after successful restore. Invoking the exact restored manifest-pinned EF 10.0.8 assembly completed migrations. No tool major version was changed.
- Renaming deliberately leaves standalone branding initials and environment allocations for application review; these are explicit checks in the reusable checklist.

A second fresh GitHub clone at fix revision `a658f9e3db76ca29fabb314393ec622617e23a9a` was renamed and independently configured again. Both host builds, all 184 renamed unit tests and 74 renamed architecture tests passed. Its new database received the full provider migrations; both hosts returned HTTP 200, normal credential login succeeded with mandatory MFA enrollment, and the renamed local database tool reported zero pending migrations for all three contexts. Later evidence-only documentation updates do not change that tested runtime source. Final merge/CI evidence is recorded in the release PR history.

Release PR #3 merged as `3ac5a29d66fdf4b46d28d16ecf3417d8c4293411`. Main architecture, frontend and backend build/test CI completed successfully. Azure migrations and deployments were skipped with the repository deployment switch still false. The existing local Docker Blazor service was refreshed to the merged managed assemblies using the existing runtime base, preserving the real API, database and Data Protection stores. Health, sign-in and framework asset requests returned HTTP 200; anonymous GraphQL remained HTTP 401. Reloading the existing signed-in Account Security page restored the session successfully.

## Disposable cleanup

Stopped only the owned test hosts, removed the acceptance Compose project/network, dropped the three explicitly named synthetic databases, and removed both clones' four generated secret stores, clone files, temporary credentials and key rings. Removed the four matching synthetic Mailpit invitation messages while retaining other messages. Verification logs and the redacted pass/fail results remain outside Git. The existing ST database still reports zero pending ControlPlane, IAM and Shared migrations. No second StarterTemplate worktree was retained.

## Remaining environment-specific sign-off

On 11 November 2026, the account owner completed both Google and Microsoft account-connection flows successfully in the existing local Docker application. The Microsoft attempt initially returned AADSTS50011; adding the Web redirect URI `http://localhost:8180/signin-oidc` to the correct Entra application registration resolved the mismatch. The existing Visual Studio HTTPS callback was retained. This confirms owner-tested account connection; a separate sign-out/provider-sign-in plus application-MFA acceptance run has not been independently observed. The disposable fixture did not copy or enable the existing application's provider credentials. Physical passkeys and external delivery/payment providers have their own acceptance scope.

Cloud/dedicated-stamp execution remains pending: deployment identities, resource/Key Vault/database access, real ingress/origins, authenticated success/failure callbacks, interrupted-run timeout/retry, and full restore/cutover/rollback with key access, private assets and queued work. Record measured recovery objectives from that deployed rehearsal.

Use the [downstream configuration and release checklist](downstream-configuration-checklist.md) for each application, environment and stamp. Keep secret values and synthetic acceptance credentials out of this record and Git.
