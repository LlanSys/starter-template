# Operational task assignment

StarterTemplate turns the dashboard's operational counts into individual work items. The workflow remains the source of truth; assignment records describe responsibility and never grant authority or bypass workflow gates.

## Queues and transitions

| Operation | Result | Required authority |
| --- | --- | --- |
| Claim | Unassigned item moves to the actor's My Tasks | Current task action permissions; eligible active account |
| Assign | Unassigned item moves to the selected account's My Tasks | Task action permissions plus the scope's assignment-management permission |
| Reassign | Item moves from the previous assignee to the selected assignee | Same management and action permissions; eligible target |
| Release own task | Item returns to Unassigned / Team Queue | Current task action permissions and ownership |
| Release another person's task | Item returns to the team queue | Management and task action permissions |
| Resolve workflow | Item disappears from active queues; assignment history is retained | Existing workflow operation and its existing business gates |
| Task reappears | New occurrence is unassigned; a previously resolved identical occurrence requires a new claim/assignment | Current authority and fresh revision |

My Tasks, Unassigned and All Team Tasks are available at `/tasks`, with an explicit category filter and server-side paging. All Team Tasks and eligible-account search require assignment-management permission. Dashboard counts exclude items assigned to other people; My Tasks counts include the current actor's assignments. Counts and pages use current source conditions, so resolving a workflow removes the task immediately on refresh. Personal authenticator setup stays under Your Actions; an unexpired support approval remains information, not assignable work.

Assignment organizes responsibility; it is not an exclusive workflow lock. A person who retains the underlying business permission can still perform that operation. There is no separate notification-dismissal or manual task-completion action that can conceal unfinished KYC or provisioning work.

## Authorization and workspace scope

| Category | Required action and destination permissions | Eligible account scope |
| --- | --- | --- |
| KYC review | `control_plane.view_kyc`, `control_plane.approve_kyc` | Platform workspace; explicit platform staff |
| Provisioning failure | `control_plane.view_tenants`, `control_plane.manage_tenant_lifecycle` | Platform workspace; explicit platform staff |
| Administrator invitation | `control_plane.view_tenants`, `control_plane.manage_tenant_administrators` | Platform workspace; explicit platform staff |
| Due legal-hold review | `control_plane.view_kyc`, `control_plane.manage_kyc_holds` | Platform workspace; explicit platform staff |
| Customer support approval | `support.approve` | The signed customer workspace |

Assigning/reassigning others additionally requires `control_plane.manage_tasks` for platform work or `tasks.assign` for customer work. Holding a management permission alone never authorizes the task category. Claiming cannot take an item already owned by someone else. Support/Demo proxy sessions cannot manage or claim these queues.

Eligible assignees must belong to the owning workspace, be active, email-confirmed, not deleted, have completed restricted administrator setup and have authenticator MFA enrolled. Platform permissions require explicit direct grants and platform identity; role labels and reserved claims on ordinary roles confer no authority. Customer permissions may come from direct grants or valid same-workspace roles in currently enabled modules. Inactive/unavailable permission definitions and module entitlements are respected.

The eligible-account dropdown is only a convenience. The assignment write repeats the live eligibility query and rejects a target who lost a required permission after being listed. It never accepts a role name, permission list or alternate workspace supplied by the client. Existing workflow APIs continue enforcing current authority and, where required, current-session MFA proof.

If an existing assignee later loses access, their affected queue disappears with that authority. An authorized manager sees Needs Reassignment in All Team Tasks and can reassign or release the item. Ownership is not silently transferred. Completion reconciliation does not read across isolated customer IAM databases.

## Source identities and persistence

ControlPlane stores OperationalTaskAssignments and append-only OperationalTaskAssignmentEvents. IAM accounts are referenced by stable IDs, without cross-database entity navigation. API responses resolve display names only within the current owning workspace. Evidence documents, submission tokens and review notes are not copied into task history.

A task key includes category, subject, occurrence ID and occurrence timestamp. KYC uses the workspace identity and creation timestamp; provisioning uses its operation ID; invitations use the current invitation revision; legal holds use their scheduled review timestamp; support approvals use the grant identity. New attempts/review occurrences therefore do not inherit old assignments. Restored work whose identical key was previously resolved is reopened explicitly on the next valid claim/assignment, with an audit event and no inherited owner.

A unique source-key index fences concurrent first claims. A GUID concurrency token and client ExpectedRevision fence stale changes. Assignment and its history event are saved in one Unit of Work commit; a concurrent loser receives HTTP 409 and must reload. An unavailable target receives HTTP 422, insufficient authority HTTP 403, and an unavailable/cross-workspace source HTTP 404.

The source query is applied before counting, sorting and materializing a page. Names and eligibility for the bounded page are batched. Search returns at most 50 eligible accounts; history returns the latest 100 changes. Persisted history is not truncated by that display limit.

The hosted completion reconciler runs every 15 seconds using TimeProvider, scans up to 100 open assignment records per category per pass, and advances a keyset cursor. It records Resolved only after the source no longer meets the task condition. A later pass retries concurrency or database failures; multi-instance writes use the same revision fencing. Workflow completion hides a task immediately on refresh even if its resolution event is still waiting for reconciliation. This worker does not provision workspaces, approve KYC, release holds or grant IAM access.

## Configuration and downstream checks

1. Apply both owning-context upgrades for the chosen provider: ControlPlane AddOperationalTaskAssignments and IAM AddTaskAssignmentPermissions. SQL Server and PostgreSQL have separate migrations. Apply the IAM upgrades to every existing IAM database, including isolated customer databases, through the owning deployment workflow. Updating the platform database alone does not update an isolated customer database. Do not reset an existing database.
2. Keep the Core module and its Tasks page available. Customer support tasks and `tasks.assign` additionally require the IAM module and its active catalogue entries.
3. Grant assignment-management permission deliberately to team leads who already have the relevant action permissions. The vendor IAM administrator role template includes `tasks.assign`. The UpgradeManagedTaskAssignmentRoles migration also backfills that permission only into an active, trusted Tenant Administrator role with the matching provisioning template and normalized identity. A role label alone, a custom template copy or a deleted role is insufficient; existing custom roles are unchanged. The local development administrator seeder follows its existing explicit platform-grant configuration.
4. Confirm account workspace, active/email/setup/MFA state and actual effective permissions before treating a person as an eligible assignee. Enrolment alone does not replace current-session MFA at the workflow API.
5. Retain the existing ControlPlane database connection and per-workspace IAM routing. No new storage provider, external service, token, secret, port or feature flag is required.
6. Verify one authorized claim/assignment, ineligible-target rejection, movement between queues, release, stale-revision rejection, workflow resolution/history and permission revocation in the target environment.
7. Keep the existing cloud release sign-off separate. Local Docker and disposable provider tests do not certify deployed cloud identity routing or operations.

New downstream task categories need a shipped definition, an authorized source projection, action/assignment permissions and workflow tests. An arbitrary catalogue record cannot create an executable task or grant access.

## Local verification — 2026-11-13

- All 35 focused integration checks passed: 29 real password/authenticator/JWT IAM and task checks, two provider-level task checks, and four fresh/upgrade migration checks across SQL Server and PostgreSQL.
- Coverage includes assignee rejection for missing permissions, absent staff identity, role-only reserved grants, wrong workspace, inactive/deleted/unconfirmed/incomplete-setup accounts and missing MFA; permission removal after eligible-account discovery; personal/unassigned movement and release; simultaneous claims; denial of stealing/managing without authority; source-attempt recurrence; paging; source resolution and history; and protected-role upgrade exclusion of custom, label-only and deleted roles.
- All 217 unit tests and 74 architecture checks passed. API/frontend builds passed with existing package/analyzer warnings. The focused formatter preservation fixtures and source/whitespace checks passed. An unrestricted working-tree whitespace check also reported two existing trailing-space lines in unrelated CreateMenu.cs; those user changes were preserved.
- A disposable browser host used the actual task/dashboard components, theme and stylesheet with synthetic sessions/backend state. Observed: eligible-account search and assignment, removal from Unassigned, claim into My Tasks, manager team visibility and release, assignment history, non-manager control filtering, read-only empty state and a 390px phone layout without horizontal page overflow. This is UI composition/interaction evidence, not a new real-account browser or external-provider walkthrough. The temporary host and isolated keys were removed.
- The local ST database on localhost:14335 was upgraded through the guarded local workflow. ControlPlane, IAM and Shared report zero pending migrations. Both Docker images were rebuilt and their containers restarted. API readiness/sign-in returned HTTP 200; anonymous attention/task APIs returned HTTP 401; the Tasks HTTP shell contained no protected queue content.
- Changes are local and uncommitted. Visual Studio needs both hosts restarted. Cloud/stamp deployment acceptance remains pending under the previously agreed local-Docker scope.
