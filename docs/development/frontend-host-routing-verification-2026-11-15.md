# Company address and QA identity acceptance - 15 November 2026

## Implemented

Company frontend origins are explicitly registered through Control Panel > Tenants > App Address. Mapping is unique, revision-protected and permission-protected. SQL Server and PostgreSQL migrations add the nullable origin and unique index. Shared/vendor portals use an explicit configuration allowlist. Blazor resolves its actual address before restoring a session and supplies that address on JSON, multipart and file requests. Unknown/inactive/wrong-stamp addresses fail closed; conflicting selectors, query links and signed sessions are rejected. SSO returns require the same active tenant's registered company origin or an approved shared origin and the exact callback route. KYC and provisioning gates remain intact.

Public discovery now has its own configurable 120-read/minute, zero-queue budget. Browser testing reproduced the previous shared authentication budget queueing metadata requests beyond the UI timeout. The new regression proves immediate discovery throttling and unchanged credential-login throttling. No credential, MFA, linking or recovery limits were increased.

The original QA account fixture names used company names as people. The two current synthetic administrators were corrected through the existing user-management API:

| Company | Person | Login | Email | Company app address |
|---|---|---|---|---|
| Alderfield QA Services | Taylor Reed | qa.alder.admin | taylor.reed@alderfield.example.test | http://alder.localhost:8181 |
| Brookmere QA Supplies | Jordan Ellis | qa.brook.admin | jordan.ellis@brookmere.example.test | http://brook.localhost:8181 |

Account IDs, separate passwords, roles and enrolled MFA seeds were preserved. Their encrypted local handoff was rekeyed to the new qa-only login names. No credential-helper safeguard was removed. Historical invitation recipients and archived acceptance results remain historical evidence, not current account-profile data. Long email summaries stay on one line with an accessible full title and full address in Account Details.

## Executed checks

- Final unit suite: 280 passed, zero failures/skips (`frontend-routing-unit-final.trx`).
- Final architecture suite: 74 passed, zero failures/skips (`frontend-routing-architecture-final.trx`).
- Broad integration selection: 48 passed, zero failures/skips (`frontend-routing-final.trx`), including real JWT/MFA platform-role checks, account-admission concurrency, routing boundaries, and fresh/upgrade/idempotence/concurrency checks on SQL Server and PostgreSQL.
- After the browser-discovered throttle fix, eight focused integration cases passed, zero failures/skips (`frontend-routing-browser-fix.trx`). There are 49 distinct passing integration cases across these two runs. This was targeted verification, not a rerun of the entire integration suite.
- Both providers' fresh and upgraded databases enforce the company-origin unique index with a real duplicate write. Historical-schema fixtures project historical columns before upgrading.
- API and final Blazor Linux images built successfully and are running locally. Existing analyzer warnings remain.
- The local SQL database was backed up with COPY_ONLY/CHECKSUM and RESTORE VERIFYONLY before its additive migration; all three contexts now report zero pending migrations. VERIFYONLY is not a restore rehearsal.
- The endpoint audit passed: 76 allocations and nine independent secret stores. Focused formatting and all 19 preservation fixtures passed; whitespace checks passed.
- Real product APIs registered both QA company origins. Fresh password and authenticator sign-in proved the correct user/tenant, separate passwords/seeds, and rejection of Alderfield's bearer session at Brookmere's origin.

## Browser observations

Both bare company sign-in URLs resolved the company without a query string, manual code or Change Organization control. The brand areas displayed AQS/Alderfield and BQS/Brookmere; Microsoft and Google appeared with the prepared form. Both accounts completed password and MFA, reached their dashboards and displayed individual names in Profile and the account menu. Company identity remained in the sidebar/Organization field. Both sessions restored correctly after the final UI update.

A Brookmere workspace query on Alderfield's host showed the cross-organization rejection with no credentials form. An unregistered .localhost host showed Organization Address Unavailable with no sign-in form. The shared localhost portal retained StarterTemplate branding and Change Organization. A real QA platform operator completed password/MFA, opened Alderfield's App Address dialog, loaded the existing mapping and saved successfully; subsequent API discovery confirmed both saved mappings.

The aliases do not match the existing localhost WebAuthn relying party. Unsupported conditional prompts are skipped and the fixed-position Passkey action is disabled there. The shared localhost origin retained its available Passkey action. Browser passkey/authenticator ceremonies and fresh real Google/Microsoft sign-in were not certified in this pass; the latter remains in Allan Run as previously agreed.

Local screenshot evidence (ignored generated artifacts):

- output/frontend-routing/st-alderfield-person-profile.jpg
- output/frontend-routing/st-brookmere-person-profile.jpg
- output/frontend-routing/st-company-app-address.jpg

No temporary credential-transfer files or helper bearer tokens are retained. Passwords/MFA material remain Windows-user encrypted outside the repository. The source working tree remains uncommitted; there was no push or cloud deployment. Real-domain DNS, TLS, ingress and provider/identity configuration require deployment-specific verification.

Use the [company-domain configuration checklist](tenant-branding-and-sign-in.md#company-app-addresses) and [current local QA credential handoff](local-qa-account-handoff.md). Existing archived packs predate these login changes; use this current handoff for the existing baseline accounts.
