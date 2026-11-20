# Organization settings and workspace sign-in

## Supported catalogue

OrganizationSettingCatalog is the vendor-owned allowlist in SharedKernel. The catalogue is code, not infrastructure configuration. Every shipped key has a default, valid options, description, API validation and a UI consumer. There is no requirement to create database rows before the first run.

| Key | Default | Consumer |
| --- | --- | --- |
| presentation.time-zone | UTC | Profile, payments, KYC review, device, support and task/staff history timestamps |
| presentation.regional-format | en-GB | Explicit date/month and number formatting, including payment amounts |
| presentation.date-format | dd MMM yyyy | Timestamp date part and calendar-date formatter |
| presentation.time-format | 24 | Timestamp time part; supported 12/24-hour choices |
| presentation.page-size | 25 | Initial page size in administration and operational listings |

Time zones use IANA identifiers on Windows and Linux. The catalogue translates Windows system identifiers before publishing choices. Supported regional formats initially include en-GB, en-US, en-KE and fr-FR. These control presentation, not translated interface resources or currency conversion. Use PresentationPreferences rather than changing process-wide culture in a pooled Blazor Server host. The service is circuit-scoped; never share one tenant's preferences through a singleton.

## UTC and TimeProvider

TimeProvider supplies the authoritative current instant through GetUtcNow. Time-zone preference is a display transformation of that same instant. It does not change the provider, stored timestamps, token/session expiry, MFA validation, invitation expiry, retention jobs, database clock stamping or timer intervals. DateOnly values retain their calendar date without time-zone conversion. Existing DateTime API timestamp contracts are interpreted as UTC; prefer DateTimeOffset for new instant contracts.

PresentationPreferences formats timestamps with an explicit numeric UTC offset so ambiguous daylight-saving times remain distinguishable. The anonymous client evidence-submission page explicitly displays UTC; it does not expose authenticated organization settings. Input parsing, wire payloads and persistence retain their existing contracts. These presentation settings are not security policy.

## Product workflow and permissions

Open Admin Center > Org Settings. All five settings are present even without overrides. Edit uses a searchable controlled selector and a green Save action. The page previews timestamp, calendar-date and number formatting. More > Reset To Default removes the override; subsequent reads use the current vendor default. Recent Changes shows the latest 100 changes with stable actor identifiers, previous/new override, reset marker and UTC-backed time.

- tenant-settings.view: catalogue and history administration.
- tenant-settings.update: save a supported override, including the first override.
- tenant-settings.delete: reset an override to default.
- The retained tenant-settings.create permission and old generic POST/PUT endpoints do not authorize arbitrary keys. Those writes return an expected business-rule response directing callers to the catalogue. Existing unrelated records remain intact and are not consumed as presentation configuration. Legacy deletion cannot bypass supported-setting reset/history.
- GET /api/v1/shared/tenant-settings/presentation requires a valid authenticated workspace session, but no administration permission. Its allowlisted response contains only these five harmless effective presentation values. It exposes no arbitrary keys, secrets or history.

Catalogue/history queries use the tenant-settings cache group and tenant mutation epochs. Save/reset invalidate that group after success. Presentation state is fetched on workspace/account changes, navigation and session refresh; a local settings save/reset refreshes it immediately. In-flight results from an old identity/workspace are discarded. An unavailable presentation request uses documented display defaults with a warning; it grants no authority. Existing sessions pick up another operator's changes on their next navigation or session refresh.

Each write locks the tenant/key inside its database transaction (SQL Server application lock or PostgreSQL transaction advisory lock). Revision tokens reject stale saves, including simultaneous first overrides. Override and append-only audit insertion commit together. A reset leaves the change history intact. Both provider migrations add OrgSettingChanges only; no existing settings or identities are removed.

## Save and reset reference slice

| Responsibility | File |
| --- | --- |
| Protected page, settings table, and save/reset orchestration | [AdminOrgSettings.razor](../../src/Frontend/Web/ST.UI.Blazor/Features/Shared/OrgSettings/Pages/AdminOrgSettings.razor), [code-behind](../../src/Frontend/Web/ST.UI.Blazor/Features/Shared/OrgSettings/Pages/AdminOrgSettings.razor.cs), [edit dialog](../../src/Frontend/Web/ST.UI.Blazor/Features/Shared/OrgSettings/Components/OrgSettingFormDialog.razor) |
| Typed client and circuit-scoped display state | [OrgSettingsService](../../src/Frontend/Web/ST.UI.Blazor/Features/Shared/OrgSettings/Implementations/OrgSettingsService.cs), [PresentationPreferences](../../src/Frontend/Web/ST.UI.Blazor/Features/Shared/OrgSettings/PresentationPreferences.cs) |
| Transport payload, catalogue, and endpoint permissions | [SaveCatalogOrgSettingRequest](../../src/Shared/ST.SharedKernel/Features/Shared/OrgSettings/Dtos/SaveCatalogOrgSettingRequest.cs), [OrganizationSettingCatalog](../../src/Shared/ST.SharedKernel/Features/Shared/OrgSettings/OrganizationSettingCatalog.cs), [OrgSettingsController](../../src/Backend/Api/ST.Api/Features/Shared/OrgSettings/Controllers/OrgSettingsController.cs) |
| Save/reset intent, validation, transaction, and cache invalidation | [SaveCatalogSetting use case](../../src/Backend/Application/ST.Application/Features/Shared/OrgSettings/SaveCatalogSetting) |
| Tenant-scoped override and append-only audit | [OrgSetting](../../src/Backend/Domain/ST.Domain/Features/Shared/OrgSettings/Entities/OrgSetting.cs), [OrgSettingChange](../../src/Backend/Domain/ST.Domain/Features/Shared/OrgSettings/Entities/OrgSettingChange.cs), [OrgSettingRepository](../../src/Backend/Persistence/ST.Persistence/Features/Shared/OrgSettings/Repositories/OrgSettingRepository.cs), [SharedUnitOfWork](../../src/Backend/Persistence/ST.Persistence/Features/Shared/SharedUnitOfWork.cs) |

The edit dialog submits the setting value and revision. Reset submits the revision to its own endpoint with its own policy. The controller derives the actor from the authenticated request and sends the same application command with the save or reset intent. The handler locks the setting for the current tenant, checks the revision, and commits the override change and audit together. The command invalidates cached settings after success. The page reloads the catalogue, history, and display preferences.

When adding a downstream feature, start with one use case and the roles, tenant scope, and records it owns. Keep the page, its interaction code, and its typed HTTP client in the owning Blazor feature. Co-locate the command, validator, and handler by use case in Application. Put the HTTP request and response in SharedKernel when both hosts use them. Keep business invariants and persistence contracts in Domain, and implement the owning context's repositories and provider migrations in Persistence. Prove the permission boundary, tenant isolation, and stale-write behavior where the use case needs it. The [neutral test-only slice](../../tests/ST.Tests.Integration/NeutralSlice/NeutralVerticalSliceTests.cs) shows how a product feature reaches the API, handler, Unit of Work, and tenant-filtered repository.

## Workspace sign-in

Workspace identifies the organization before credential lookup. It is not an access permission. The same email can belong to independent accounts in different workspaces. Continue to validate credentials, MFA, tenant state and live permissions at the API boundary.

A resolved organization uses its tenant display name and initials in the auth-card brand area. There is no duplicate Signing In To line. Change Organization opens the Organization Code fallback selector. Changing organization clears typed credentials and cancels a pending conditional passkey request. Manual entry asks for the identifier from the invitation/sign-in link; Continue resolves the active workspace before showing credential/provider controls. Invalid or unavailable workspace links must not fall back to a previously remembered organization's credential form. The selected workspace is persisted in protected session storage and the URL is synchronized after manual selection. Registered client origins retain the existing cross-origin redirect behavior.

Supply /iam/sign-in?workspace=<identifier> links in customer-facing onboarding material and bookmarks. Locally the platform is default, the completed QA customers are qa-alder-01 and qa-brook-02. Company app addresses now support [explicit frontend origin mapping](tenant-branding-and-sign-in.md#company-app-addresses); DNS/TLS and deployment routing must be configured and verified for real domains. There is no public all-customer directory, email-based automatic account linking, or new global identity model.

## Extending a downstream application

Add a catalogue definition with bounded options/default and implemented consumer, then add validation, permission/isolation/reset tests and update this document. Do not add dormant keys for SMTP, SSO credentials, connection strings, arbitrary security policies or unsupported business features. Use the existing tenant registry for organization identity rather than creating a second organization-name source. Personal preferences may override tenant defaults in a future feature; this slice supplies tenant defaults only.

## Verification

The current change adds unit tests for UTC, Nairobi conversion, DST, DateOnly stability, regional formatting and invalid choices; integration tests cover permission floors, tenant/cache isolation through real JWT sessions, atomic history/reset and simultaneous writes on SQL Server/PostgreSQL. Record executed results in the implementation completion note; source presence alone is not a test result. The earlier operator acceptance pack is historical and remains unchanged.

Executed local results are recorded in [15 November verification](organization-settings-verification-2026-11-15.md); use the [Allan Run supplement](organization-settings-acceptance.md) for an independent run. The workspace header and Profile use the matching protected selection's display name when the current-user contract has no workspace label. That cached display metadata grants no authority.
