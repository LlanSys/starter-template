# Platform IAM acceptance

Date: 2026-11-11. Platform operations and tenant IAM are different boundaries. A role label such as System Administrator is never sufficient authority.

## Current permission model

Ordinary IAM permissions come from current roles or direct grants, subject to module availability and active definitions. Reserved `control_plane.*` operations require direct grants, explicit `platform_staff` identity and current MFA. Reserved role claims cannot confer platform authority. The retired `control_plane.manage` key grants nothing.

`PlatformPermissions` defines independent view/create/edit/lifecycle, stamp, module, support, audit, Demo and KYC operations. KYC collection, download, review, final approval, retention policy and legal holds are separate capabilities. `control_plane.view_kyc` supplies a minimal KYC queue and case view without requiring tenant-management access. The Control Panel shell accepts any supported capability and renders the corresponding sections; each route, button and REST/GraphQL operation checks its own permission. Tenant IAM roles and tenant isolation remain unchanged.

The upgrade expands direct legacy management grants only for explicitly identified platform-tenant staff. Existing KYC approvers receive the equivalent KYC operations; combined trusted managers/approvers receive retention/hold administration. Management alone never acquires KYC approval. Non-staff and role-only legacy claims gain nothing. Both provider migrations remove the retired direct and role claims. Rollback of permission assignments requires the pre-upgrade backup; Down recreates definitions, not historical grant intent.

## Repeatable real sign-in matrix

The automated matrix is [PlatformIamSignInTests](../../tests/ST.Tests.Integration/ControlPlane/PlatformIamSignInTests.cs). It creates synthetic identities in a disposable SQL Server container, assigns real roles and claims, calls password sign-in, enrolls an authenticator, and signs in again using a fresh first-factor challenge and OTP. It supplies no X-Test identity/permission headers and does not mint its own bearer tokens. The host forwards Bearer requests to the application's JWT handler.

| Identity setup | Tenant reads | KYC case / approval | IAM reads | Create role |
| --- | --- | --- | --- | --- |
| Administrator label only | Denied | Denied | Denied | Denied |
| Staff only | Denied | Denied | Denied | Denied |
| Direct operation grants without staff | Denied | Denied | Denied | Denied |
| Reserved grants through roles | Denied | Denied | Denied | Denied |
| Staff with view_tenants | Allowed | Denied | Denied | Denied |
| Staff with retired manage | Denied | Denied | Denied | Denied |
| KYC-only approver | Denied | Allowed | Denied | Denied |
| KYC collector | Denied | Case read / approval denied | Denied | Denied |
| Retention operator | Denied | Case read / approval denied | Denied | Denied |
| Staff with tenant read + KYC operations | Allowed | Allowed | Denied | Denied |
| IAM reader role | Denied | Denied | Allowed | Denied |
| Platform/KYC grants plus IAM writer role | Allowed | Allowed | Allowed | Allowed |

Further cases verify a customer administrator cannot enter the platform, removing approval authority denies an existing token immediately, removing staff membership removes platform navigation/API access without refresh, and logout makes the session unusable. First-factor MFA challenges cannot be used as full sessions. Before enrollment, platform identities must receive MfaEnrollmentRequired from both login and current-user responses so Blazor directs them to security setup.

The review found that issued claims previously included reserved role permissions even though the live API discarded them. Claim issuance now matches the direct-grant rule. Staff using custom role names now receive enrollment guidance consistent with the API gate.

Run from the StarterTemplate repository:

```powershell
dotnet test tests/ST.Tests.Integration/ST.Tests.Integration.csproj --no-restore -p:UseSharedCompilation=false --filter FullyQualifiedName~PlatformIamSignInTests
```

## Browser acceptance and verification scope

Use disposable accounts and data. Check visible navigation/action controls, direct protected routes, password and MFA entry, and denied writes through the normal API. Cover an unprivileged administrator label, an IAM reader, a platform manager without KYC approval, and a KYC reviewer. Record rendered UI evidence independently of API results.

The updated real sign-in matrix passed 11/11, including enrollment-guidance assertions. Full regression and browser evidence are recorded below after completion. This evidence does not certify cloud identity providers, physical passkeys or dedicated ingress.

## Completed local evidence: 2026-11-12

The regression run passed 190 unit tests, 74 architecture tests and 64 integration tests; one existing RabbitMQ outbox transport test was skipped. The new real password/MFA matrix contributed 11 passing cases. API and Blazor builds passed with existing warnings.

A disposable SQL Server database and synthetic identities were used for the browser walkthrough. Blazor ran on its own port and connected through a loopback bridge to the real API pipeline. Password, authenticator enrollment/sign-in, JWT/session validation and authorization remained active. Test-only rate-limit capacity was raised for repeated fixture setup. This was local acceptance, not deployed cloud or rate-limit certification.

| Browser identity | Observed outcome |
| --- | --- |
| Administrator label without permissions | Signed in with password/MFA; no Control Panel navigation; direct Control Panel visit showed Page unavailable. |
| IAM reader | Signed in with password/MFA; roles/users readable; user listing had View actions and no create/edit controls; Control Panel route denied. |
| Platform manager without KYC approval | Control Panel and tenant management visible; KYC actions absent from the tenant menu; direct KYC route denied. |
| KYC reviewer | KYC document page accessible; synthetic evidence uploaded, accepted with feedback, and its saved review history reopened. |

The browser run exposed a missing named KYC policy in the Blazor composition root. It initially caused a direct-route error. Registering that policy fixed resolution while requiring authentication, staff membership, manage and approve_kyc. Both denied-manager and allowed-reviewer paths were retested. Document acceptance did not constitute workspace KYC approval. Private download authorization remained covered by the API integration tests.

Temporary fixture accounts, workspace and database belong only to the disposable environment. Cleanup removes its synthetic blob and hosts. No real customer's role, credentials or KYC decision was changed. The existing single StarterTemplate worktree remains in use.

## Granular permission follow-up: 2026-11-12

The previous coarse-permission limitation is closed for the shipped platform operations. `PlatformPermissions` supplies 24 supported operation keys, with no legacy wildcard. The API checks current identity/grants; the Blazor shell and pages use native operation policies. JWT profile responses now carry `HasMfaProof` from the validated current session; the Blazor identity bridge projects that proof. Authenticator enrollment alone is not accepted as proof of this sign-in.

The real password/authenticator matrix now covers 15 cases, including collectors, KYC-only approvers, retention operators, a tenant reader, IAM roles, old/manage-only grants, role-only reserved grants, customer identities and live revocation. Claim-injected transport tests remain supplemental, including readonly REST/GraphQL parity.

A disposable browser environment exercised actual password and authenticator sign-in for four custom-role identities, with normal sign-out between accounts:

| Profile | Observed browser behavior |
| --- | --- |
| KYC reviewer/approver | KYC-only Control Panel; successful synthetic KYC approval with an external review reference; tenant-management direct route denied. No collection/upload authority was added. |
| Retention operator | Policy fields editable; successfully published a new version, while the existing case kept its original policy; placed and released a reasoned legal hold and saw the 90-day review date. |
| Tenant reader | Tenant table available; create/edit/lifecycle actions absent; KYC policy direct route denied. |
| Staff membership only | No Control Panel menu; direct route denied. The header link now uses the same native access policy. |

All browser identities, workspaces, policy edits and hold operations above were synthetic and isolated from the working local ST database. Your local default remains the 24-month general-business policy. This is local acceptance, not cloud or live external-provider certification.

Latest regression: 202 unit tests, 74 architecture tests, and 70 integration tests passed; one existing RabbitMQ outbox transport test remains skipped. Both provider migration paths passed fresh/upgrade/idempotent checks. The affected MFA/GraphQL/retention regression and the independent erasure/restore check also passed.

## Shipped administration page permissions (2026-11-14)

`AdministrationPageAccess` supplies operation floors shared by API navigation and native Blazor `Page:` policies. Menu configuration can narrow visibility; it cannot make a shipped administration page usable without the corresponding read operation. IAM permits the matching Roles, Permissions, Users and User Devices tabs independently. A users-only reader need not load role metadata. Read-only settings/catalogue access shows no Add action and disables mutation slots.

The Admin Center refreshes its options when effective access changes. IAM replaces a revoked active tab, disposing its obsolete results. Staff forms survive an unchanged-authority session refresh and close when the identity/session/MFA/proxy/permission context changes. Backend permission checks remain independent.

When a downstream app adds an administration capability, extend the shared page-operation contract and repeat limited-role navigation, direct-route denial and API denial checks. Module/page availability alone is not operation authority.

## Shared interactive permission projection

CurrentUserPermissions is shared by inline controls and native interactive claims. Supported explicit platform grants require platform identity and remain independent of tenant-module unavailability; unavailable ordinary permissions and retired platform keys are excluded. Tests cover all three boundaries. Backend operation permissions and MFA policies remain authoritative.
