# Operator acceptance: two real onboarding runs

Prepared 2026-11-14. This workbook supports repeatable local business acceptance. The agent's completed operations and remaining checks are recorded separately in `output/pdf/operator-acceptance/qa-results.csv` and `live-run-evidence.json`. The independent Allan Run pack starts with every case NOT RUN.

## Platform staff prerequisite is implemented

Use **Control Panel > Platform Staff** to create/invite operators and edit their explicit platform membership and supported direct operation grants. The workflow records an immutable reasoned audit, limits delegation, revokes changed accounts' sessions and protects the last verified administrator. See [platform staff administration](platform-staff-administration.md).

Nine generated local QA operators and two customer baselines are available. These were created through the product API/UI, not SQL claim edits. Retrieve their local-only credentials and current authenticator codes with [the handoff helper](local-qa-account-handoff.md). Existing owner credentials and MFA were preserved.

## Environment and identity setup

- Existing local Docker: API http://localhost:8180, UI http://localhost:8181, Mailpit http://localhost:8030. Check health, scanner and migration status before starting.
- Retain the existing bootstrap administrator for setup/recovery. Use limited operator accounts for the actual work.
- Each person/profile has its own password and MFA enrollment. Store credentials and recovery material in your password manager; no password, MFA QR/seed, token, submission link or invitation link belongs in the result log.
- Use separate browser profiles for platform, client submission, Tenant A and Tenant B. Explicitly sign out before switching personas within a profile.
- Use the existing default-pooled-stamp for BOTH tenants. This exercises the shared-infrastructure isolation boundary. It does not certify dedicated deployment.
- All example.test mail remains local in Mailpit. It is not a real Google/Microsoft identity. Exercise external providers separately with actual provider test identities.

## Reusable platform operators

Use eight limited profiles plus the generated QA staff administrator and existing setup owner. Platform grants in account-matrix.csv use suffixes for the exact control_plane. keys. They require explicit staff identity and current MFA; ordinary role claims cannot confer that authority.

| Profile | May do | Must not do |
| --- | --- | --- |
| QA Onboarding Officer | Create/edit workspace; select existing stamp/modules; manage administrator invitations | Approve KYC or administer platform staff |
| QA KYC Collector | View KYC; upload; issue/revoke client submission links | Approve KYC or change retention policy |
| QA KYC Reviewer | View case; download cleared evidence; review and approve KYC | Create tenants or administer staff |
| QA Support Operator | Request/enter approved, scoped, expiring customer support | Enter a tenant without customer approval |
| QA Read-Only Auditor | Supported tenant/stamp/module/KYC/support/audit reads | Write or download original evidence |
| QA Retention Operator | Retention/hold views and permitted hold operations | Approve KYC or administer users |
| QA Lifecycle Operator | Customer lifecycle and module operations | Approve KYC or administer staff |
| QA Platform Task Manager | Assign supported tasks to eligible operators | Grant access through assignment |
| QA Staff Administrator | Configure staff and supported platform operations | Bypass customer support consent or KYC |

Collection and approval are separated operationally in this exercise. A universal enforced maker/checker rule is not implemented; add one if the product requires it. The roster does not pretend it exists.

Each tenant gets three identities: its initial administrator, a coordinator and a reader. Tenant roles contain only implemented, available and delegatable permissions. Select the actual catalogue entries for the described duties; an attractive role label is not proof of access.

## Two successful onboarding runs

| Run | Fictional customer | Method | Actual result |
| --- | --- | --- | --- |
| 1 | Alderfield QA Services / qa-alder-01 | Platform collection, complete documents, straightforward pooled onboarding | Completed through real API/password/MFA; detailed results in agent pack |
| 2 | Brookmere QA Supplies / qa-brook-02 | Mix platform/client collection; correction cycle; encrypted-file rejection; invitation resend | Completed through real API/password/MFA; detailed results in agent pack |

Set MaxUsers=3 and enable Core and IAM. Use distinct first administrators: Taylor Reed (`taylor.reed@alderfield.example.test`) for Alderfield and Jordan Ellis (`jordan.ellis@brookmere.example.test`) for Brookmere. Use separate credentials and MFA enrollments for these separate accounts. Do not pre-create those tenant administrator accounts: actual invitation acceptance must create restricted setup and finish it.

Start with a fictional sales handover and Control Panel registration. Public Start Trial intake and an automatic trial clock are not shipped. Record trial intent as QA notes; do not mark unimplemented public intake or billing as passed.

### Run 1: complete evidence

- [ ] Onboarding officer registers qa-alder-01 with the intended administrator and existing pooled stamp.
- [ ] Workspace is pending KYC. No customer administrator authority or welcome delivery is available prematurely.
- [ ] Collector uploads Alder files 01-05 into their matching document inputs. Run 2 exercises the client channel as well.
- [ ] Observe Pending/Scanning/Clean; pending files cannot be downloaded or accepted.
- [ ] Reviewer compares company, representative and authority across records, then accepts the complete synthetic evidence with feedback.
- [ ] Reviewer deliberately approves workspace KYC with a reference such as QA-ALDER-REVIEW-01 and notes that the evidence is fictional QA data.
- [ ] Observe pooled initialization/readiness and welcome invitation delivery in Mailpit.
- [ ] Intended administrator opens its own invite, validates credentials, enrolls and verifies MFA, and completes setup.
- [ ] Sign out and sign in afresh using workspace qa-alder-01. Confirm tenant administration and no platform access.
- [ ] Administrator creates the coordinator and reader with limited roles. Sign in separately as both and exercise permitted/denied actions.
- [ ] At three non-deleted human accounts, a fourth admission is rejected. Inactive accounts still count.
- [ ] Supported audit/status views identify the correct workspace, actors and sequence.

### Run 2: corrections and recovery

- [ ] Repeat registration for qa-brook-02 on the SAME pooled stamp, using its distinct initial-administrator email and independent credentials/MFA.
- [ ] Mix platform uploads and a client submission link to exercise both channels.
- [ ] Use Brook files 01-04 and 06-business-address-needs-correction.pdf. The draft should scan Clean; the HUMAN reviewer requests its missing address details.
- [ ] Client sees intended feedback for its own link issuance. Upload Brook 05-business-address.pdf as the correction; earlier feedback/history remains.
- [ ] Optionally upload 07-business-address-password-protected.pdf. It must be Blocked and unavailable for download/acceptance. Request an unlocked replacement. The fixture password is training-only.
- [ ] Resolve evidence under consideration; deliberately approve QA KYC with reference QA-BROOK-REVIEW-01.
- [ ] Verify readiness and invitation delivery. Resend before acceptance, prove the previous link is invalid, and complete the CURRENT invite.
- [ ] Complete credentials and MFA, then sign out and sign in afresh with workspace qa-brook-02.
- [ ] Create/sign in as its limited coordinator and reader; check the fourth-human-account limit again.
- [ ] Confirm this run did not change Alder's users, settings, permissions or availability.

A clean antivirus result is separate from Accepted/Changes requested and from final workspace KYC approval. These are real application decisions over explicitly synthetic data, not verification of a real company.

## Operate both tenants after onboarding

- [ ] Same email selects distinct IAM accounts by workspace. Credentials and MFA do not silently transfer between them.
- [ ] A session/user from A cannot read or update B users, roles, settings or KYC document identifiers through changed URLs, selectors, headers or request bodies. Check backend denial as well as hidden menus.
- [ ] Verify different generic setting values in A and B, cross-ID write denial and deletion isolation. The template currently supplies key/value storage; a vendor setting catalogue, application consumer and reset-to-default are separate product work and must not be marked passed by a storage-only test.
- [ ] A reader can perform its allowed reads but cannot create users, edit roles/permissions, change settings or reach platform routes.
- [ ] Revoke a coordinator permission while signed in; the next protected request is denied and stale UI results clear.
- [ ] Deactivate a signed-in tenant user; old sessions cease working. Reactivation preserves credentials and MFA.
- [ ] Support operator requests minimal access to A. Deny entry before customer approval; approve as A's customer administrator; perform one allowed action; revoke while support is active. B remains unaffected.
- [ ] An authorized lifecycle operator suspends A; A is blocked while B remains usable. Reactivate A without bypassing KYC. Remove any temporary elevated duty afterward.
- [ ] Assign a KYC task to an eligible reviewer. Verify the assignee queue changes; assignment to an unauthorized auditor is rejected and grants no new authority.
- [ ] On a clearly tagged QA case, place/review/release a reasoned legal hold without changing global retention defaults.
- [ ] Keep both baseline companies Active with approved KYC, completed administrator setup and intended limited users.

Do deletion/recovery, actual invitation expiry, forced scanner outage, backup restore and dedicated-runtime exercises as separate operational scenarios. Preserve these two successful baselines. Do not move the shared machine clock or shorten the platform-wide retention policy to accelerate a test.

## Evidence, defects and pass criteria

Use qa-results.csv. Allowed statuses: NOT RUN, PASS, FAIL, BLOCKED and NOT APPLICABLE. Explain NOT APPLICABLE. An unmet prerequisite is BLOCKED, not PASS.

A successful run requires actual KYC approval, readiness, invitation acceptance with MFA, fresh administrator sign-in and at least one correctly limited member sign-in. A tenant row or one successful response is insufficient.

Record check ID, persona, workspace, expected/actual result, tester/time, sanitized screenshot and trace ID where available. Add reproduction and issue references for failures. Do not save authorization headers, MFA material or unredacted HAR files. Retest failed and dependent steps after a fix.

Keep separate results for platform IAM, tenant IAM, cross-tenant isolation, browser UX, external providers and cloud/dedicated certification. Two happy paths do not erase permission-denial failures.

## Pack contents

The local output/pdf/operator-acceptance folder and output/pdf/startertemplate-operator-acceptance.zip contain twelve synthetic PDFs, document-manifest.json, account-matrix.csv, editable qa-results.csv and this checklist. The files contain no credentials, MFA seeds or invitation/submission tokens. Completed checks identify their verification method; the separate Allan Run sheet remains blank.

For the existing completed live QA tenants, use [the current account handoff](local-qa-account-handoff.md#distinct-customer-administrators-15-november-2026-follow-up); archived acceptance packs retain their original shared-email isolation scenario.
