# StarterTemplate UI conventions

The Access Catalog is the administration listing reference. A compact admin-center layout and a shared MudProviderHost inform the presentation. Preserve StarterTemplate's own IAM, tenant model and effective menu filtering.

## Listing composition

Pages opened from Admin Center include **Back to Admin Center** above their card and use Title Case page headings. Support Access follows this rule too.

1. Put the listing in one white, lightly bordered card.
2. Place its neutral title and right-aligned primary Add action in the same header. Use the Add icon for entry actions.
3. Put optional Title Case tabs immediately below that header. Changing tabs changes the header action where appropriate.
4. Put search and filters below tabs, above the table.
5. Use compact white table rows, a subtle neutral header shade and horizontal overflow inside the data container. Set a wide data minimum using `--st-table-min-width` on MudTable; do not set a minimum width on the outer component, which would move paging controls off-screen.
6. Show record counts and paging together below the table. Counts and empty results are informational, not warning-colored alerts.

An embedded tab page omits its own title and creation action. Its host owns those controls. Standalone routes retain their header. Do not reproduce the active tab's label below the tabs. Forms and case decisions retain task-specific structure rather than being forced into listing tables.

Create actions, row actions and routes retain their existing permission checks. A visible menu or button never grants API access. Platform and tenant administration continue to use StarterTemplate's established boundaries.

## Theme and controls

MudProviderHost.razor.cs owns the runtime palette and typography. CSS compatibility tokens reference its MudBlazor palette variables, with matching fallbacks, so branding changes do not require a competing palette in individual pages. Primary actions use a restrained slate blue (#334E68); headings use dark slate text. Page backgrounds are neutral (#F5F6F8), card/table surfaces white and table headers subtly shaded (#F2F4F6). Keep success/warning/error semantic colors; destructive confirmation uses the MudBlazor error palette. Focus indicators remain visible. Avoid per-page bright heading overrides, transparent data tables and colored record counts.

Use consistent Material Add/View/Edit/Delete icons through TableRowActions, with extra actions in its compact menu. Put informational indicators such as the required-module lock inside the same shared action strip so rows align; use st-actions-column for the header and cell. In the role header, Apply Template precedes the primary Add Role action. Keep headings restrained and forms responsive. The KYC collection grid remains three columns on desktop, two on tablets and one on phones. Native details controls collapse upload history. Preserve unsent selections when moving between case tabs.

## Navigation and template-only screens

Admin Center uses compact icon links generated from the API's effective AdminCenter menu, rather than a table of navigation links. Keep Admin Center at the sidebar bottom. Control Panel is a navigation tile hub, following the BaseTemplate reference. Deployment Stamps, Tenants, Modules, KYC Cases, KYC retention, managed support, audit and Demo each keep their individual permission checks. Navigation, Identity & Access and Access Catalog tiles link to the existing workspace IAM routes; they do not create a new global IAM model. Former /control-panel?tab= links redirect to the corresponding standalone listing. Keep those listings in the shared white table-card composition, with Back to Control Panel above the card.

Remove the old Resources/Solution Overview footer from the product shell. Legacy /overview, /features, /counter and /weather URLs redirect to the dashboard. Existing catalogue/database records remain intact for compatibility; retired template route choices are excluded from the UI. The old Showcase module is excluded from the main module list and new tenant-module assignment choices; existing assignments remain visible for operators to remove deliberately. Payments and the vendor Demo workspace remain supported capabilities.

## Verification

Build the affected Blazor host. Check the theme/card/header/tab/filter/pager hierarchy through representative platform and tenant administration pages. Verify create actions after tab changes, role-limited visibility, table horizontal overflow and phone/tablet layouts. Recheck KYC file selections and existing authentication redirects where the shared shell changes. Document exactly which browser cases were observed; source review and compilation do not establish full visual acceptance.

## Local acceptance on 2026-11-12

The Blazor build passed from an isolated output directory because the user's running Visual Studio host locked its normal executable and assembly. The host was left running. All 74 architecture tests passed. Existing package/analyzer warnings remain; no dependency upgrade is part of this UI pass.

A disposable SQL Server/API/Blazor browser environment used actual password sign-in and authenticator MFA. Observed cases included the shared Control Panel header and three creation actions (stamp/tenant dialogs and module form), compact Admin Center links, Access Catalog colors and data, and IAM Roles/Users tab actions. Source review covered the remaining administration listings, module detail catalogue, KYC case shell, shared providers, navigation and table wrappers; this is not a claim that every dialog and state was manually viewed.

Phone (390px) and tablet (900px) checks confirmed no page/workspace horizontal overflow. The phone module table retained its data scroller while its pager stayed inside the card. The retired Showcase option disappeared from the new module list. All four legacy template URLs redirected to Dashboard. After removing the fixture user's grants down to platform identity plus stamp view, the browser showed one supported Control Panel tab, View-only row actions and no create/edit/tools controls. The temporary viewport was reset.

Legacy source-file deletion and catalogue-seed changes were rejected by automatic approval review as a broad potentially breaking removal. The implemented alternative is UI retirement through reversible redirects/filtering; it preserves stored records and assignments. No backend schema, IAM authority or tenant model change was needed for this pass.

The final Docker Blazor image was rebuilt and started. Its sign-in endpoint and API readiness both returned HTTP 200. The disposable review environment, keys, synthetic credentials, containers and listeners were removed. Changes remain uncommitted; unrelated user edits were preserved.

## Platform overview (2026-11-13)

The dashboard uses four compact metric cards per row on desktop, two on smaller screens and one on phones. Cards have a thin semantic accent; use the existing restrained MudProviderHost palette, neutral canvas and white surfaces. The Control Panel hub uses the same responsive grid for icon navigation tiles. The left header shows Platform / Control Panel or Workspace / workspace name; account actions stay at the right, without a role label. Support/Demo context and return controls remain intact. Platform sidebar navigation shows Dashboard and Control Panel, with the effective Admin Center entry at the bottom; customer, Demo and support sessions retain workspace module navigation.

GET /api/v1/control-plane/tenants/dashboard requires control_plane.view_tenants. The database aggregates customer workspaces only (excluding the platform and IsDemo records) and returns no workspace details or infrastructure configuration. Invitation aggregates additionally require control_plane.manage_tenant_administrators and are null otherwise. Pending invitations include Pending and Claimed records; delivery retries mean a pending attempt with no delivery receipt and a cleared delivery lease (a reported unsuccessful attempt), not a permanent failure. Awaiting KYC means any customer without recorded KYC approval, independent of lifecycle status. Pending provisioning, provisioning and failed provisioning are separate counts. The dashboard failure shortcut applies the existing persisted ProvisioningFailed status filter with a clear-filter action.

Stamp metrics use the existing stamp-view API and permission. ST supports Pooled and Isolated tiers, not enabled/disabled stamp lifecycle states; show total/pooled/isolated counts. Do not copy unsupported BT metrics or expose customer counters to a stamp-only or KYC-only operator.

### Verification for the platform overview

The frontend compiled successfully. All 74 architecture checks passed using a separate in-repository Overview build configuration, leaving the running Visual Studio hosts untouched. An initial architecture run from external temporary output failed source-root discovery; the in-repository rerun resolved that runner-location issue.

A disposable copy of the actual frontend components with a synthetic authenticated session and synthetic backend responses was viewed in the browser. Observed: desktop dashboard metrics, desktop/tablet Control Panel tiles, phone grid/no page overflow, rounded active sidebar navigation, a standalone stamp listing with the shared action cells and Back to Control Panel link, and the former ?tab=stamps bookmark redirect. This validates layout and navigation composition; it is not a new real-account browser sign-in or provider walkthrough. The stopped preview and synthetic-session source files were removed.

All 17 focused PlatformIamSignInTests passed against disposable SQL Server databases using actual password plus authenticator sign-in and JWT requests. Coverage includes dashboard denial without staff identity/current tenant-view authority, limited invitation aggregates, live revocation and the customer-versus-Demo counting regression. No live customer records were modified by these tests.

The final API and Blazor Docker images were rebuilt and restarted. Sign-in and API readiness returned HTTP 200; the new dashboard endpoint returned HTTP 401 without authentication. Existing package/analyzer warnings remain. Changes are local and uncommitted. Visual Studio needs an API and frontend restart to use this revision.

## Menu icon selection

Create/Edit Menu and Access Catalog's Customize Icon form share MenuIconPicker. It searches all installed MudBlazor Material styles (Filled, Outlined, Rounded, Sharp and TwoTone), previews each result and the selected icon, and searches custom catalogue labels as well as stable icon names. Results are bounded to 30 visible entries; refine the search rather than loading thousands of SVG elements into a dropdown. Menu forms retain StarterTemplate's registered-page selection, role/permission visibility, hidden metadata and tenant boundaries.

Access Catalog / Menu Icons combines the shipped MaterialIconKeys allowlist with saved metadata. Uncustomized definitions have no database ID and no edit/delete action; Customize Icon creates an optional metadata record. Existing saved keys, labels, activation state and IDs remain intact. Short Filled aliases such as Menu suppress duplicate Filled.Menu defaults. Catalogue creation accepts real Material icon names, not raw SVG or invented names. Material-name casing is accepted only for the menu-icons catalogue; other catalogue key rules are unchanged. No bulk icon rows or schema migration are required. Regenerate MaterialIconKeys.txt whenever the MudBlazor package changes.

Icon parity verification: the Blazor host compiled, 23 focused icon/menu-visibility unit tests passed, both SQL Server catalogue integration tests passed, and all 74 architecture checks passed. The actual shared picker and Customize Icon dialog were viewed in a disposable component host: custom-label display, name/style filtering, icon previews, Outlined.AccountCircle selection and TwoTone.Home/label submission were observed. The label field updates immediately so Save uses the entered label. This component check does not claim a new signed-in full-page IAM walkthrough. The disposable host and its source files were removed after stopping it.

The final Docker API and frontend images were rebuilt together and started successfully; API readiness and the sign-in page returned HTTP 200. This pass adds no database schema changes. Restart both Visual Studio hosts to load the new API catalogue and frontend picker there. The changes remain local and uncommitted.

## Authentication startup and attention items

Branding belongs inside every authentication card, including MFA, invitations, recovery and the wide public KYC card. AuthBrand is shared. The anonymous HTTP shell contains only a public loading card; protected content stays unprerendered. Its placeholder is removed as soon as the interactive router appears. The router restores protected browser state before deciding on access; it uses internal navigation to sign-in rather than reloading the document.

Sign-in publishes its form after browser workspace selection and provider discovery finish. Provider discovery runs independently of workspace I/O and has an eight-second limit; failure leaves password/passkey sign-in available with an explicit retry. Workspace selection revalidates the selected workspace's own stored session. Only an approved cross-origin workspace redirect reloads the document. Same-origin initialization no longer reloads it. No provider is enabled from a frontend-only flag. The external Google Fonts request is removed so the local fallback font does not swap after first paint; dashboard entrance fades are also removed.

Needs Your Attention is a live snapshot, not a persistent notification inbox. The authenticated /api/v1/dashboard/attention endpoint checks module/page access, then the native operation policies for each category before querying it. Reserved platform categories additionally require MFA proof. Action categories include KYC approval outstanding (which does not imply evidence is complete), failed provisioning, failed/expired administrator invitations on eligible active customer workspaces, the next three due legal-hold reviews, customer approval requests scoped to the signed workspace, with approved unexpired support access owned by the current platform actor displayed separately as information. All counts come from current records and omit zero-count items. The user's own optional authenticator setup cue is based on their authenticated profile. Role labels never grant authority.

The UI filters returned items again using current effective permissions, clears stale results when authority changes, cancels obsolete loads and rejects older responses. Routine session refreshes with unchanged authority do not clear/repaint the list. Manual Refresh reloads the snapshot; there is no new background polling loop, email, push delivery or dismissal store. Empty/error/loading states are distinct. Workflow actions still require their existing API permissions; a notification grants no access.

Startup investigation verified two avoidable sign-in transitions: an initially empty provider list populated after the form rendered, and forced same-origin reloads after workspace resolution. Both are removed. A redundant RequireAuthenticated loading frame is also skipped when the interactive router has already restored an authenticated session and both MFA/setup gates are clear; native page authorization and the existing redirect checks remain in place. The sign-in/loading card reserves the same dimensions. In the four-second provider-delay browser fixture, both states measured 656px high at the same 32px top position; inputs/providers were 0/0 during preparation and 4/3 after discovery. This is controlled local evidence, not a production latency measurement.

Verification for this pass: frontend builds passed, 18 real password/MFA/JWT IAM tests passed, including attention permission filtering, pre-MFA denial, cross-workspace support counts and role revocation; 74 architecture checks passed. Browser checks used actual frontend sources with synthetic data and a deliberate four-second provider delay. Observed: public loading state, complete provider reveal, discovery failure with working password/passkey controls and retry, in-card MFA branding, populated attention items, limited-access filtering and phone layout without horizontal page overflow. No new real-provider SSO sign-in was performed. BT's formatter preservation fixtures passed and the scoped formatting pass covered the affected authentication/dashboard sources and tool; this is not a claim that the entire historical repository was reformatted.

Final local delivery: the API and frontend Docker images were rebuilt/restarted, both readiness/sign-in checks returned HTTP 200, anonymous attention access returned HTTP 401 and the protected dashboard's HTTP response contained the public loading shell without attention content. The synthetic preview, its source files and isolated keys were removed. Temporary viewport changes were reset. The focused formatter scopes covered 36 source/tool/test files in total, with no remaining differences in those scopes. Existing dependency/analyzer warnings remain. No database migration, real provider reauthentication, cloud deployment, commit or push is part of this pass. Restart both Visual Studio hosts to load the same API/UI revision there.

### Startup click gate

Because the router is not prerendered, a page shows only the static loading card until the Blazor Server circuit has connected and Routes has resolved the workspace and session. A click in that window used to land on markup with no event handlers and be dropped silently. Downstream teams reported this as "the first click after the page loads is ignored". It was reproduced on the template's own sign-in page by clicking while "Opening the application…" was showing.

`wwwroot/js/appStartup.js` loads before `blazor.web.js`. Until an element with `data-app-ready` exists, it marks `<html>` with `app-starting` and `aria-busy` (progress cursor) and holds pointer clicks. Routes sets `data-app-ready` once routed content or the host-error card can take input. On release, if the last held click landed where a form field now sits, that field receives focus. A held click is never replayed on a button or link, because the user had not seen that control. The reconnect modal and `#blazor-error-ui` are exempt. Keep `data-app-ready` on Routes, and keep the script ahead of `blazor.web.js` when changing `App.razor`. A page that shows its own loading state after the router is ready, such as sign-in provider discovery, must show controls only when they work.

### Passkey autofill lifetime

Sign-in starts a conditional (autofill) WebAuthn request. The browser ends it with `NotAllowedError` when the server's timeout elapses (Fido2's 60-second default), when another tab or frame starts its own WebAuthn request, or when the user dismisses the picker. `webauthn.js` treats that as a normal end and logs it at debug level, not as an error. The next time the page is visible and focused, it calls `SignIn.RestartConditionalAssertionAsync`, which asks the server for fresh options because each challenge is single-use. Starting a new conditional request always cancels the pending one first, since a browser allows only one. A restart is skipped while the page is busy, while the workspace is being changed, and after the page is disposed. Real-authenticator passkey acceptance is still recorded separately.

### Actionable work and shared responsibility

Needs Your Attention separates Your Actions (currently the signed-in account's authenticator enrollment) from a Team Queue. Shared operational cases are not personal assignments, and possession of a view permission does not make someone responsible for acting. Each item states the recorded condition and gives a specific next step. No deadlines or assignments are inferred where the workflow has none.

The API requires both target-page access and action authority: ViewKyc + ApproveKyc for outstanding KYC, ViewTenants + ManageTenantLifecycle for provisioning failures, ViewTenants + ManageTenantAdministrators for invitations, ViewKyc + ManageKycHolds for due hold reviews, and support.approve within the signed workspace for support requests. Legal-hold review never implies that a hold should automatically be released. KYC collectors and read-only operators retain their permitted pages and metrics without receiving approval or provisioning action tasks.

Available Support Access is a separate informational panel, not work requiring a decision. It includes only the current platform actor's unexpired approvals, requires ViewSupport + ManageSupport and does not grant or automatically enter a support session. The same stale-authority fencing and live backend permission checks apply to every section.

Refinement verification: all 22 real password/authenticator/JWT PlatformIamSignInTests passed, including KYC read-only/collector omission, provisioning reader versus lifecycle operator, signed-workspace support requests, role revocation, and current-actor/unexpired support information. The shared item-kind contract has its own JSON enum converter and round-trips through the actual API. All 74 architecture checks passed. The final frontend compiled; existing package/analyzer warnings remain. Scoped formatting and whitespace checks passed.

A disposable host rendered the actual dashboard, shared item list, theme and stylesheet with synthetic sessions/backend data. Desktop and 390px phone views showed reason/next-step copy and separate Team Queue/Available Support Access sections without horizontal page overflow. Read-only access removed action rows, and the personal MFA cue appeared under Your Actions. This is component/layout verification, not a new full real-account browser walkthrough. The temporary host and isolated keys were removed.

Both local Docker images were rebuilt and restarted. API readiness and sign-in returned HTTP 200, anonymous attention requests returned HTTP 401, and the dashboard HTTP shell contained no protected attention content. Restart both Visual Studio hosts to use this revision. Changes remain local and uncommitted; no schema migration or cloud deployment was introduced.

## Assignable operational work

The Team Queue now represents genuinely unassigned operational items. My Tasks contains the current actor's assigned items; personal MFA setup remains Your Actions. The Tasks link opens the shared card/table composition, with My Tasks, Unassigned and authorized All Team Tasks tabs, a category filter, record counts and server-side paging. Assignment uses a searchable eligible-account picker and a separate recent-history dialog. Available Support Access stays informational.

Claim, assign/reassign and release operate on an individual source occurrence. Reassignment controls require the appropriate task-management permission plus action authority. The API rejects ineligible assignees even if the dropdown previously listed them. Authority changes clear old rows and close open assignment/history dialogs. An assignee who loses eligibility is marked Needs Reassignment for authorized managers. Task completion follows the real workflow; there is no arbitrary Complete/Dismiss button. See [operational task assignment](../architecture/operational-task-assignment.md) for scope, history and configuration details.

Task-assignment verification: 35 focused integration checks, 217 unit tests and 74 architecture checks passed. SQL Server/PostgreSQL migrations and task persistence, concurrency, recurrence, live eligibility, protected-role upgrade boundaries and completion history were verified. The actual task components were exercised with synthetic browser sessions on desktop and phone; assignment, claim, release, history and permission-filtered controls were observed. The guarded local database upgrade and final Docker health/anonymous-access checks passed. See the operational assignment guide for exact scope and remaining cloud acceptance.

## Shared action, form and account standards

TableRowActions always reserves View, Edit, Delete and More slots in that order. Unavailable actions remain disabled; an empty More menu is a disabled grey icon. Tables with informational indicators reserve the additional indicator slot across their rows. Operational-task and support lists use their own fixed workflow slots, with existing permission/state checks controlling availability. Disabled controls grant no authority.

Entry actions use Add, while persistence forms use a green Success Save button and Save icon. Delete actions and delete confirmations use Error. Other workflow actions retain their specific meaning, such as Approve, Connect or Verify. Button and action-menu labels use Title Case without changing the underlying action keys. Dialog headers use a neutral shaded surface and divider. Status chips use light semantic tints and dark readable text rather than filled high-contrast badges.

Profile uses a compact identity summary, readable account-status badges, a styled private-photo selector and account details. Account header context shows Profile or Security for account routes; support and Demo notices retain precedence. The MudMenu account dropdown closes on navigation, click-away or Escape. Connected sign-in actions use provider brand icons and a wrapping row with spacing. Existing authentication, password-confirmation, private-asset and session boundaries remain intact.

Navigation Item Module is a selector of available implemented modules, followed by filtered registered Page choices. The registered page owns its module; editing a catalog-owned page cannot move it to another module. The dropdown does not grant module entitlement or route/API access.

### UI polish verification on 2026-11-13

The final Blazor revision compiled with zero errors; 147 existing dependency/analyzer warnings remain. All 217 unit tests and 74 architecture tests passed during this pass. The focused source formatter covered 66 files, its 19 Razor preservation/idempotence fixtures passed, and the later browser-driven corrections passed their own focused formatting and whitespace checks.

A real signed-in Docker session was used to observe Profile, its readable status chips, Account/Profile and Account/Security header context, account-menu opening/navigation dismissal/Escape/click-away behavior, provider brand icons and a 12-pixel connection-button gap, the shaded Add Role dialog with green Save icon/button, and Navigation Item Module choices (Core Workspace, Identity and Access, Payments). Dialogs were cancelled; this review did not save administration records, change permissions, upload photos or reconnect providers. The session later expired.

Final exact action alignment and Delete/Deactivate confirmation colors were checked in a disposable, database-free host using copies of the actual shared components, theme and stylesheet. All four icon centers matched across fully available, partially available and disabled rows, including mixed informational indicators. A 390-pixel phone viewport had no horizontal page overflow. This final component check is distinct from the real-account page checks above; it does not certify every authenticated page or role combination.

Only the Docker Blazor image was rebuilt/restarted for this pass. API readiness, sign-in and the protected profile HTTP shell returned HTTP 200; no private profile data appeared in the anonymous shell. No backend/schema/configuration change was needed. Restart the Visual Studio frontend to load these changes there. Changes remain local and uncommitted.

## Tenant company identity

Use the existing tenant display name and derived initials in the auth/sidebar brand positions. The vendor platform retains the product identity. Remove repeated Signing In To copy; the shared-entry fallback uses Organization Code and Change Organization. Keep an unresolved/loading brand neutral, bind displayed company metadata to the current/explicit tenant, and retain independent backend authorization. See [tenant branding and sign-in](tenant-branding-and-sign-in.md).
