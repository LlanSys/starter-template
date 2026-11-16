# Closing KYC verification and retention gaps

Date: 2026-11-14. This is an implementation and acceptance checklist. The upload slice includes size/extension/signature checks, private quarantine, checksum-bound real ClamAV scanning, durable retries and human review. OCR and independent authenticity verification remain open. See [the implemented safety and account-admission runbook](account-admission-and-kyc-safety.md). Enabled application retention/deletion and its policy screen are described in [KYC retention policy and operations](kyc-retention-policy.md); actual cloud provider/backup verification remains pending. No destructive production work is authorized by this document.

## Decide what automated verification means

Treat these as separate results: file safety, extracted text, consistency checks, independent-source verification, and the final KYC decision. A clean scan says nothing about document authenticity. OCR produces candidate data, not proof. A successful sign-in or email confirmation proves a sign-in method or mailbox control, not the organization's legal identity or a representative's authority.

Close verification when the configured workflow reliably performs its specified checks, records outcomes and routes exceptions for review. Human KYC approval can remain the final decision. Unattended approval needs a separately approved low-risk decision policy and evidence; it is not implied by enabling automated checks.

## Verification work and acceptance

- [ ] Define accepted evidence types, mandatory checks, authoritative sources, match/confidence thresholds, freshness and exception handling for the downstream product.
- [x] Add persisted processing states and results, bound to immutable document ID, checksum and, where applicable, blob version. Keep file-safety state separate from Accepted/Changes requested review status.
- [x] Quarantine new and existing originals until required safety checks finish. Gate downloads and automated processing on an explicit clean result. Missing, timed-out, unscannable and encrypted/unsupported files require a deliberate exception path; they must not become clean by default.
- [x] Implement a real scanner adapter with durable polling/leases, bounded retry/backoff, timeout/recovery and revision-based concurrency. Bind every result to the original checksum; stale leases/results cannot approve a replacement or erase newer state. This adapter uses direct INSTREAM responses, not external callbacks.
- [x] Configure the real local ClamAV adapter for Azurite development.
- [ ] Certify the selected deployed scanner endpoint or another cloud adapter. Microsoft [Defender for Storage](https://learn.microsoft.com/en-us/azure/defender-for-cloud/on-upload-malware-scanning) supplies Azure on-upload scanning with consumable results. Azurite storage compatibility does not supply this cloud scanning service.
- [ ] Add IronOCR or another selected extraction adapter only when licensed and useful. Record engine/version, extraction outcome, corrected values and the reviewer/source of corrections. Use authorized provider-neutral contracts.
- [ ] Implement required registry/identity/source checks where suitable services exist. Record source, query reference, outcome, UTC time and freshness without retaining unnecessary provider payloads or secrets.
- [x] Present file-safety outcomes, failures and required follow-up to reviewers and appropriate client-facing status. Keep private reviewer notes separate from feedback intentionally shown to clients.
- [ ] Prove clean, rejected, inconclusive, unsupported, timeout, provider outage, duplicate event, stale-result and wrong-workspace behavior. Use benign security-test fixtures and synthetic identities; never live client evidence in test fixtures.
- [ ] Certify the actual cloud event identity, private container, scanner results and failure paths before cloud sign-off. Local adapter tests cannot certify a live provider.

Do not call authenticity verification complete if only antivirus and OCR are implemented. State exactly which checks and providers have been certified.

## Retention/deletion implementation

The product owner authorized a practical general-business default and a UI for amendment on 2026-11-12. See [the implemented schedule and workflow](kyc-retention-policy.md). This replaces the earlier unconfigured/disabled-worker proposal.

The application provides versioned policies, case deadlines, legal holds with review dates, recoverable expiry, fenced deletion leases, provider-aware retries, private erasure receipts, metadata cleanup, upload reservations and orphan reconciliation. It does not bypass legal holds, infer relationship end from suspension or claim Azure copies are gone merely because Delete Blob returned success.

Cloud sign-off still needs the chosen storage account's version/snapshot/soft-delete and immutability behavior, managed-identity permissions, the configured 35-day backup horizon, independent ledger preservation and a restore/rollback rehearsal. Automated authenticity verification remains a separate capability; the retention workflow does not certify documents.
