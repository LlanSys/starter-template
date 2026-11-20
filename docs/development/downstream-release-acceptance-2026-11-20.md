# Downstream baseline release acceptance — 20 November 2026

## Revision and decision

The code candidate is `654a260e4708a30e85753453f8b8a912a519ecc1` on `codex/downstream-architecture-adoption`, based on current main `b87c0c6c298d215f3807308635e0b9e59729203b`. This is the source revision tested in a clean local clone. The later evidence commit changes documentation only; use the code candidate when reproducing these results.

The candidate is qualified as the pinned downstream source for local product development and is ready for PR review. Qualification covers the local evidence below. Product deployment acceptance remains separate; this work does not merge or deploy the candidate.

The change enforces the existing project/context boundaries, groups organization-settings save/reset as the reference slice, stages IAM account events in the account transaction, and improves rename and local allocation tools. The two IAM outbox migrations are additive. Existing routes, permission keys, Shared outbox history, scheduler identity and protected-data identities remain compatible. Review [transaction and delivery boundaries](../architecture/persistence-standards.md#transaction-and-delivery-boundaries) before upgrading an existing database.

## Clean-clone configuration

A local Git clone was checked at the candidate revision before rename, with no source working files, ignored output, application environment file or host secrets copied. The normal rename script created **AdoptionRelease**, namespace **AR**, and new, distinct API and Blazor UserSecretsId values. Product identity, JWT issuer/audience and Data Protection application name were renamed before the first run.

| Concern | Acceptance configuration |
| --- | --- |
| API and Blazor | Native HTTP 6357 and 6306; reserved HTTPS 8329 and 8249. Separate launch profiles. |
| Database | Fresh `ARRelease_654a260e` on the workstation SQL Server at 14335. ControlPlane, IAM and Shared histories applied in order with EF 10.0.15. No existing ST or BT database was upgraded. |
| Cache and API key ring | Own Redis on loopback 6412, Compose project `cg-adoptionrelease`, own volume, generated password and `AdoptionRelease-DataProtection-Keys`. |
| Blazor key ring | Own temporary persistent directory and renamed application name. |
| Signing and bootstrap | Newly generated signing key and local test credentials. Explicit platform bootstrap, password reset disabled. Bootstrap disabled and its password removed from the runtime environment before restart verification. |
| Private assets | Distinct `arrelease-profile-654a260e` and `arrelease-kyc-654a260e` containers, private access. Initialized before readiness acceptance. |
| Shared local infrastructure | SQL Server, Mailpit at 1030/8030, Azurite at 10020–10022 and ClamAV at 13315. These are deliberate local infrastructure shares; application databases, cache, keys and asset containers are separate. |
| Optional capabilities | Google/Microsoft sign-in disabled with no provider credentials. Live smoke configuration disables messaging and Quartz; the full suite separately enables actual RabbitMQ and tests durable work and both database providers. |

The focused endpoint audit passed against the clone's supplied Compose file and repository hosts. Existing unrelated workstation allocation conflicts remain warnings in focused mode. The complete workstation audit retains its stricter behavior.

## Checks

| Check | Result |
| --- | --- |
| Package policy | 13 projects and 87 central versions pass; strict CI restore with force/no-cache passes. |
| Pinned tools | Local tool restore passes; the normal `dotnet tool run dotnet-ef --version` command reports 10.0.15. |
| Policy regressions | Two accepted fixtures and 27 rejected-policy cases pass. |
| Rename regression | Preview, renamed paths, host identities and references, stable identical rerun, environment/output exclusions pass. |
| Required source checks after integrating main | API build and all 70 architecture cases pass with `--no-restore -p:UseSharedCompilation=false`. |
| Clean renamed builds | API and Blazor Release builds pass. Existing analyzer warnings remain. |
| Complete renamed unit/architecture suites | 281 unit and 70 architecture cases pass. |
| Complete renamed integration suite | All 122 cases pass, zero failed or skipped, in 14.26 minutes. Runs without a filter, with real RabbitMQ enabled and an eight-minute per-test hang timeout. |
| Earlier integration run | All 122 cases pass with no skips on the pre-dependency-update baseline. This is supplementary evidence; the renamed candidate report passed the release gate. |
| Provider migrations | Live clone applies all three fresh SQL Server histories. Candidate integration includes SQL Server/PostgreSQL fresh/upgrade checks, account/outbox rollback and restart delivery, and settings audit/concurrency. |

The full integration selection includes the disposable neutral product slice through HTTP, handler and repository, with authenticated own-tenant persistence, foreign-tenant hiding, missing-permission denial and anonymous denial. Its test authentication and test-only endpoint are explicit fixtures. No additional business module ships in the platform and no platform-internal edits are needed to add that slice.

## Live API acceptance

The clean clone's API live/readiness checks return 200, and Blazor serves the sign-in route with 200. This is HTTP and API evidence; it does not replace a signed-in browser walkthrough.

A generated platform administrator completed password login, TOTP enrollment and a fresh password/TOTP challenge. Restricted enrollment and first-factor tokens could not access administrative operations. A verified session could access the permission-protected platform endpoint, and an unknown tenant was hidden with 404.

Two synthetic customer tenants were then created through the normal platform API. The local operator explicitly approved each with a labelled synthetic evidence reference. Their normal durable welcome invitations arrived in local Mailpit. Each account accepted its delivered invitation, proved its password, enrolled TOTP, completed a fresh MFA challenge and completed administrator setup. A completion retry in the setup session passed. Customer sessions could use their own tenant settings and were denied platform operations with 403. These are synthetic local reviews, not customer KYC evidence.

For the reference slice, tenant A saved page size 50 and tenant B saved 10. Repeated reads kept both values separate. Tenant A's token with tenant B's registered identifier was rejected with 403. A stale revision returned 409. Reset restored A to 25 while B stayed 10; audit history contained two entries for A and one for B. Anonymous catalogue access returned 401.

After bootstrap was disabled, the API restarted with the same independent key ring. Both existing customer sessions remained usable, then both customers completed fresh password/TOTP login. Existing-session checks precede replacement login because the configured single-session policy invalidates the prior session when another login succeeds.

The supervised walkthrough took **10.5 minutes** from recording the clean clone revision to verified platform password/TOTP sign-in, **35.8 minutes** to completed two-tenant save/reset isolation, and **37.8 minutes** through restart acceptance. These measurements include source/contract review and probe corrections. They are observed walkthrough durations, not unattended setup timings.

Manual work consisted of choosing local identities/ports and provider settings, generating secrets, allocating the independent cache/key ring/database/private containers, applying migrations, explicitly enabling then disabling bootstrap, and exercising synthetic onboarding through the public APIs. The initial readiness check correctly identified missing private containers. Probe corrections used the supported `NamespacePrefix` rename parameter, required `AccessToken` refresh field, expected unknown-tenant 404, and setup-session/single-session rules; no runtime code changes were needed.

## Reproduction and artifacts

Start from the pinned code, then follow [starter setup](starter-setup.md) and the [downstream configuration checklist](downstream-configuration-checklist.md). Allocate new resources before starting hosts. Restore the repository-pinned tools rather than upgrading their majors.

```powershell
git clone https://github.com/LlanSys/starter-template.git AdoptionRelease
Set-Location AdoptionRelease
git checkout 654a260e4708a30e85753453f8b8a912a519ecc1
./scripts/rename-template.ps1 -NewName AdoptionRelease -NamespacePrefix AR -Confirm:$false
./scripts/check-package-policy.ps1
dotnet tool restore
$env:CI = 'true'
dotnet restore AdoptionRelease.slnx --force --no-cache
dotnet build src/Backend/Api/AR.Api/AR.Api.csproj -c Release --no-restore -p:UseSharedCompilation=false
dotnet build src/Frontend/Web/AR.UI.Blazor/AR.UI.Blazor.csproj -c Release --no-restore -p:UseSharedCompilation=false
```

Run the complete unit, architecture and integration projects under `tests/AR.Tests.*` in Release with `--no-restore -p:UseSharedCompilation=false` and save TRX reports. Enable the actual RabbitMQ check using the local messaging guide/script and configured credentials. Apply the three chosen provider histories to a fresh database, configure the hosts through the renamed secret identities or per-process environment, initialize the private containers, and exercise the API journeys above. Keep bearer tokens, passwords and invitation/authenticator material out of reports and source control.

Local artifacts reside under `ST-Release-01a1fa5882927d35be953be6df64b01e` in the Windows temporary directory. The renamed clone is `AdoptionRelease`; suite reports are in `tests/`, and sanitized configuration/live-auth/onboarding/isolation/restart JSON records are beside it. Raw logs, credentials and message contents remain local. Source checks are under ignored `tmp/release-validation/current-main`. The committed report records outcomes without secret values.

The integration TRX has SHA256 `66BF11A7480C2F883B22E55C2511D1B86714B64728F0B445D90B76B2148EB3EC`. Unit and architecture TRX reports confirm 281 and 70 passes with no failures or skips. The temporary API, Blazor and acceptance Redis container were stopped after verification. Their own synthetic database, private containers, key volume and local evidence are retained for inspection; the existing application hosts and databases were not replaced.

## Product deployment acceptance

Each downstream product must complete its own [configuration and deployment checklist](downstream-configuration-checklist.md), including trusted ingress/origins, deployed key continuity and encryption, live configured identity/email/payment/storage providers, physical passkeys where enabled, monitoring/load objectives, backup/restore and rollback, and dedicated-runtime provisioning where applicable. Local SQL Server/PostgreSQL and transport tests do not certify these environments.

The Alderfield signed-in browser settings check remains deferred at the user's request after rejected authenticator codes. This release does not change that account or claim a completed Alder browser session. Google/Microsoft provider sessions, physical devices and deployed operations remain open when enabled by a product.
