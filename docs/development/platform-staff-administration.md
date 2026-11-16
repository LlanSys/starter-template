# Platform staff administration

Platform operators are managed at **Control Panel > Platform Staff** (`/control-panel/staff`). Customer roles remain scoped to their own workspace. A role name, ordinary role claim, or matching email never creates platform authority.

## First administrator and upgrades

Fresh development bootstrap grants explicit staff membership and the supported platform permissions. Upgrades add `control_plane.view_platform_staff` and `control_plane.manage_platform_staff` only to active human accounts in the platform workspace already designated staff and holding every previous platform operation grant. Limited operators, ordinary administrators, inactive identities, proxies and customer accounts receive no automatic elevation. This is a conservative migration, not a general role-to-staff conversion. Confirm a usable administrator before rolling out to a deployed environment.

## Operator lifecycle

1. Open Platform Staff with `control_plane.view_platform_staff`. Adding, changing and inviting requires `control_plane.manage_platform_staff` and current authenticator MFA.
2. Add the name, unique platform username and email. Apply a vendor access profile, review its explicit checked grants and supply the business reason. Profiles are reusable presets, not authority-bearing role names. A manager cannot delegate a grant they do not currently hold.
3. Save creates an active but unverified account without a password. Use its **More Actions > Send Invitation** action. Local email is captured by Mailpit; production uses the configured approved email provider.
4. The recipient accepts the protected, expiring invitation, sets their own password, signs in and enrolls/verifies their own authenticator. Before that completion they cannot perform platform operations. Staff accounts cannot disable mandatory MFA or have an ordinary IAM operator enroll an authenticator on their behalf.
5. Edit the explicit grants, membership or active status with a required reason. Every change terminates that account's existing sessions and refresh tokens; it must sign in again. Removal of membership clears its platform grants.
6. View the most recent 100 access changes from the row's View action. History is tenant-scoped, append-only (updates and deletion are rejected by IAM persistence), and records stable target/actor IDs, reason, old/new membership and activity, and the exact old/new grants.

A retry of invitation delivery rotates the security stamp and invalidates the previous link. Account and invitation-request audit persist before delivery; delivery failure leaves a retriable pending account. This staff path uses the existing IAM invitation adapter with explicit manual retry. The tenant initial-administrator invitation has its separate durable delivery worker.

## Security and concurrency

Writes acquire a transaction-owned IAM authority lock, then re-read the actor's current account and direct grants. This prevents two concurrent removals from eliminating the last usable administrator. A usable manager is an active, nondeleted human in the reserved platform workspace with verified email, MFA enabled, explicit staff membership and the staff-management grant. Pending invitations do not count as recovery administrators.

Edits require the current account revision. Audit records, membership/grant changes, stamp rotation and session revocation share one IAM transaction. Ordinary IAM account, role, permission and activation screens reject staff targets. Identity optimistic concurrency also rejects stale writes across a concurrent promotion. Account identity or tenant relocation is not part of staff administration.

All API operations enforce permissions independently of UI visibility. Customer, support-proxy and demo-proxy identities cannot use this workflow. Native Blazor route policies protect the screen. Do not put reserved platform grants into ordinary tenant roles or direct-user delegation.

## Operational checks

- Maintain at least two verified administrators with separate credentials and authenticators before production operation.
- Assign narrow collector, reviewer, onboarding, support, auditor and retention duties. Do not give every operator the administrator preset.
- Email delivery failure: refresh the row, correct provider configuration and retry the invitation. Do not send a password or use password reset as initial onboarding.
- Lost authenticator: use the approved account-recovery process; do not disable mandatory MFA through database edits. Keep recovery ownership outside a single person's device.
- Read the acceptance workbook and preserve separate results for automated integration tests, actual local operations, browser checks, external identity providers and cloud certification.

## Authenticator request throttling

Authentication runs before request limiting so only validated identity claims are used for account-level budgets. Authenticator management/status uses the authenticated user within the workspace; anonymous verification remains IP-limited. Exhausted authenticator requests return HTTP 429 with Retry-After immediately instead of queuing for a five-minute window. This prevents one operator enrollment from stalling other staff. Do not disable MFA or rate limiting to run acceptance tests.

Routine refreshes with the same effective identity and authority preserve an open staff form. Identity, workspace, session, MFA, proxy or permission changes cancel dialogs and clear obsolete results. The API remains the authority for every submission.

Ordinary people administration and credential entry require Human account kind; internal support/Demo proxies stay in their dedicated workflows. Interactive permission projection must preserve explicit supported platform grants independently of tenant-module unavailability.
