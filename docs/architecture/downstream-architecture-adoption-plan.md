# StarterTemplate architecture and downstream delivery plan

This plan makes StarterTemplate the starting point for new LlanSys applications. A developer should be able to start from an identified revision, give the application its own identity and local configuration, verify the platform, and add the first product feature. The product owns its business model and operations from that point.

This is the implementation map for the StarterTemplate architecture work. It compares StarterTemplate with the 20 October 2026 General Architecture Reference, records which decisions to retain, and defines evidence for each phase. The [platform completion checklist](vendor-demo-tenant-completion-checklist.md) still tracks its own release and deployment gates. The [downstream checklist](../development/downstream-configuration-checklist.md) remains the setup and environment checklist.

## Starting point and execution rules

- [PR #5](https://github.com/LlanSys/starter-template/pull/5) is the merged platform baseline. Its head was `8ab1954` and GitHub backend, frontend, architecture and dependency checks passed. The optional container workflow skipped. At implementation kickoff, record the actual merge/base SHA and current working tree before editing. The previous clean-clone walkthrough covered an older revision.
- Take each phase as a reviewable change with its own evidence. Record status, revision, checks, exceptions and next action in the phase ledger below. When a phase exposes a behavior defect, isolate and fix it with a focused regression rather than mixing the behavior change into a move.
- Follow [AGENTS.md](../../AGENTS.md). Changes to conventions update the canonical instruction, the relevant architecture guide and the guardrail together. Prefer short using directives, injected `TimeProvider`, cancellation, stable .NET 10 dependencies and layouts the repository formatter can check.
- Keep current public routes, permission keys, payloads, table and migration identities, scheduler names, and protected-data application names stable during structural moves. Treat changes to these as compatibility decisions with an upgrade path.
- Keep one active StarterTemplate checkout. A disposable downstream clone is a release test. Product implementation, including any LlanSacco adoption work, belongs in its own workspace.

## What StarterTemplate already does well

| Keep | Existing strength and evidence to carry forward |
| --- | --- |
| Domain-neutral platform | IAM, ControlPlane and Shared work without dependencies on Banking, HR or another product's entities. A test-only neutral slice proves how to extend the template. Keep sample business code in test fixtures or documentation. [Feature convention](feature-folder-convention.md) |
| Tenant and IAM boundaries | The platform distinguishes vendor, customer and demo identities. It checks the signed tenant against routing context, protects tenant reads and writes, and derives permissions from current state. Preserve permission-based delegation, mandatory administrator MFA, support scope, expiry and revocation, and the distinct pooled and dedicated stamp policies. [Multitenancy strategy](saas-multitenancy-strategy.md) · [Operator evidence](../development/operator-acceptance-results-2026-11-15.md) |
| Customer onboarding and operations | Preserve mandatory organization KYC, private evidence and collection links, scanning, audited decisions and retention, gated provisioning, secure administrator invitations, and operational assignment eligibility. Preserve automatic company-address discovery, the shared-portal fallback and independent account identity. [Onboarding workflow](tenant-onboarding-workflow.md) · [Hostname verification](../development/frontend-host-routing-verification-2026-11-15.md) |
| A single Blazor Web host | Keep Razor pages, components and UI contracts in `ST.UI.Blazor`, with typed HTTP calls to API. Keep the anonymous shell free of protected state and preserve restored session behavior. RCL is an option when there is a concrete second Razor consumer or a separately distributed component library. [UI and API boundary](ui-to-backend-flow.md) |
| Lean dependency graph | Domain has no direct solution project references, and Persistence already references Domain only. Keep shared HTTP DTOs in `ST.SharedKernel` without backend references. Preserve the current server-side `ST.SharedKernel.Validation` boundary while checking where validators belong. [Domain project](../../src/Backend/Domain/ST.Domain/ST.Domain.csproj) · [Persistence project](../../src/Backend/Persistence/ST.Persistence/ST.Persistence.csproj) |
| Repeatable engineering and operations | Keep SQL Server and PostgreSQL migration parity, explicit local upgrades, provider configuration and test coverage, persistent Data Protection keys, tenant-aware caches, central package management and tooling. Preserve the existing GraphQL deployment-stamps example as an optional transport reference. [Database migrations](../development/database-migrations.md) · [Package policy](../development/package-management.md) · [Downstream checklist](../development/downstream-configuration-checklist.md) |

Local operator and integration evidence establishes how these flows behave under the conditions tested. A production deployment still has its own ingress, identity, provider, load and recovery acceptance.

## Architecture decisions for downstream development

1. **Use a modular monolith for the backend.** Start with the existing layer projects and ownership under `Features/{Context}/{Feature}`. Blazor remains a separate HTTP client and host. Add a product context when it has its own business language or data ownership. Revisit a module assembly or separate service when an actual dependency, scaling, operational or isolation requirement justifies it.
2. **Keep module ownership explicit.** A module owns its invariants, writes, audit facts, messages and private code. Other modules call a documented application entry point, reference a stable identifier, or consume a published event. A reporting read can deliberately span modules with tenant scope and permissions. It does not gain write authority over the source data.
3. **Keep layer dependencies enforceable.** Align the check with actual project references and compiled type dependencies. Keep persistence ports implemented by Persistence in Domain. Keep external capability ports and workflow orchestration in Application. Inventory Infrastructure → Persistence and provider-specific handlers before deciding whether to narrow them. Treat identity-provider integration as an explicit boundary.
4. **Keep data and transport contracts clear.** Use feature-owned HTTP DTOs, thin MediatR commands where the request shape fits, server-side validation and domain invariants. Use separate form or provider models for different semantics. Use a named, tenant-scoped query returning a materialized projection for sensitive or complex reads. Keep existing generic repository callers until a specific migration gives a measurable boundary or maintenance benefit.
5. **Make reliability part of a slice.** Identify the caller's tenant and actor, what must commit together, which side effects happen after commit, and what retries or duplicate deliveries do. Record durable delivery intent with the owning write where the outcome requires it. Add provider-specific locks, retries or idempotency only where the workflow needs them.
6. **Keep shared platform capabilities configurable.** Use existing module entitlements, organization settings and provider configuration at their respective boundaries. Feature availability and user permissions are separate checks. Keep secrets, provider endpoints and other infrastructure settings in deployment configuration. Keep product rules in the product.

## Incremental implementation map

### Phase 0 Baseline and ownership inventory

- [x] Verify the landed baseline and record existing CI and local evidence. Note unfinished provider and deployment acceptance separately.
- [x] Map IAM, ControlPlane and Shared. Record each context's owned data, public operations, known cross-context callers and the boundary around jobs, storage and cache. Inspect actual `.csproj` references, handler and UI placement, and the current database and event paths.
- [x] Compare each proposed change against a maintenance, reliability, isolation or onboarding problem. List the existing conventions and compatibility surfaces affected.

**Exit.** Reviewable as-is ownership map, starting SHA and a short list of changes justified by observed gaps. This phase should need no runtime changes.

### Phase 1 Enforce the existing architecture

- [x] Reconcile `AGENTS.md`, the feature convention and architecture test descriptions with the actual graph. Add checks that Persistence directly references Domain only and cannot use Application or shared HTTP DTO types. Verify Blazor stays on its HTTP boundary and Domain stays free of transport-project dependencies.
- [x] Encode the actual permitted cross-context links identified in phase 0. Start with proven boundaries such as product modules staying out of IAM and handlers staying out of another context's repositories. Record any necessary allowed dependency with its purpose. Avoid a brittle rule based on folder names alone.
- [x] Make the distinction between feature-owned components and app-wide infrastructure explicit. Keep provider registration, global middleware, shell layouts and reusable utilities in their natural common locations.

**Exit.** API and Blazor build. Architecture checks verify the documented dependencies and catch representative forbidden dependencies. Existing routes, contracts, permissions and persistent identities remain compatible.

### Phase 2 Build one reference slice with existing behavior

- [x] Use organization settings save/reset as the reference. Put the page and adjacent logic with the owning Blazor feature where practical. Group the command, handler and validation by use case where they change together. Keep shared DTOs and Domain/Persistence boundaries in their existing projects.
- [x] Preserve public routes, authorization, defaults, concurrency revisions, tenant-scoped audit and cache invalidation. Keep EF migration history intact. Use the company-address slice as a second reference for a thin command wrapper if its example is needed.
- [x] Document the slice's file map and a short recipe for adding a downstream feature. Keep the [neutral test slice](../../tests/ST.Tests.Integration/NeutralSlice/NeutralVerticalSliceTests.cs) as the executable proof of a new feature without shipping another business module.

**Exit.** An existing settings journey passes browser verification. Both provider integration cases verify save, stale-write conflict, audit and reset. The neutral slice still verifies authenticated access and tenant boundaries. A reviewer can trace a change from UI to data access from the feature name.

### Phase 3 Verify transactions and durable work

- [x] Inventory commit and dispatch paths in the bounded-context units of work, outbox and jobs. Pick the first path with observed ambiguity or missing failure evidence. Record atomicity, locking, tenant scope and behavior after a process restart.
- [x] Prove the wrapper keeps business and audit writes together where required. Prove an outbox-backed operation can resume after commit and safe delivery can repeat. For cross-context onboarding, verify retries and incomplete-state recovery across the existing boundaries.
- [x] Fix actual defects with focused tests. Business rules and provider calls belong in their owning workflow or adapter. Avoid broad repository replacements and changes to stable migration history.

**Exit.** Explicit transaction and delivery guidance backed by targeted rollback, retry and tenant-scope tests. Test both supported databases for changed persistence semantics. Record external provider evidence under its own conditions.

### Phase 4 Make the clone path repeatable

- [x] Walk through [starter setup](../development/starter-setup.md) and the [downstream checklist](../development/downstream-configuration-checklist.md) on the candidate. Identify repeated manual steps, defaults still pointing at ST, and checks that work only on the original machine.
- [x] Improve the existing rename, endpoint audit and setup tools where the walkthrough shows a repeatable gap. Prefer validated inputs, actionable errors, safe reruns and placeholder configuration. Keep credential creation and cloud targets under deliberate product control.
- [x] Verify clone identity, remote, host secret IDs, ports, origins, Compose names, volumes, databases, signing configuration and key-ring boundaries. Apply fresh provider migrations. Verify blank optional provider credentials do not block the supported local baseline.
- [x] Record the starting ST commit and how to take later platform fixes. Start with focused changes and documented backports. Consider distributing a library later only when multiple products have a real shared release and versioning need.

**Exit.** A clean clone can reach a working local sign-in and first product slice from documented setup and product-specific configuration. It can run beside ST without sharing application secrets or data. No edits to platform internals are necessary to add the reference product slice.

### Phase 5 Release the downstream baseline

- [ ] Review the staged change, run package policy and strict restore, build API/Blazor, and run unit, architecture and relevant provider integration tests. Run the full integration suite for the release candidate and record intentional external-provider skips.
- [ ] Create a disposable clean clone at the exact candidate SHA. Rename it, configure separate local resources, verify health, apply migrations and perform real local password/MFA and two-tenant isolation checks. Add a disposable product slice through normal entry points and confirm permission enforcement and tenant-scoped persistence.
- [ ] Capture the revision, configuration inventory without secret values, expected test result, measured setup time, any manual steps and remaining environment checks. Mark the release candidate as the downstream source once the evidence is reviewed.
- [ ] Keep deployment acceptance visible per product and tier. Verify deployed ingress, key continuity, live configured identity and providers, monitoring/load objectives, restore/rollback and dedicated runtime provisioning as applicable before real customer use.

**Exit.** A reviewable pinned build baseline and repeatable clean-clone evidence. The configuration checklist distinguishes local working setup from each product's deployed release obligations.

## LlanSys product boundaries

Use the same platform bootstrap across the catalogue. A capability belongs in StarterTemplate when it has genuinely reusable semantics and can be maintained independently of a product's business rules. Shared infrastructure such as email, storage or provider-neutral payment adapters can stay configurable. A product owns its workflows, data, entitlement catalogue and regulatory decisions.

| Product | Product-owned features to add after baseline setup |
| --- | --- |
| LlanSacco | Member onboarding and KYC, savings, lending, check-off, ledger, authorization of financial actions and reconciliation. Adopt platform improvements in the existing SACCO workspace. |
| LlanAssure | Agency/broker operations, clients, policies, renewals, claims and commissions. |
| LlanPOS | Sales, stock, terminals, receipts, shifts and settlement or reconciliation as needed. |
| LlanPay | Merchant onboarding, payment orchestration, provider reconciliation, settlement and payment-specific risk controls. An existing payment adapter is a starting integration seam. |
| LlanGrade | Courses, assessment, rubrics, grading, moderation and publication. |
| LlanHealth | Clinical, patient and facility workflows, sensitive data access and sector integrations. |

Where products eventually integrate, agree on a versioned API or event and keep the authoritative record in the owning product. Start the first downstream feature with its tenant and authorization decisions, use case, persistence and provider migrations, public contract, UI entry point, and practical acceptance test. Product KYC, reporting and retention rules are product work even when they reuse platform mechanisms.

## Scope controls

- Keep the current web host, stack and database-provider support. Revisit RCL, extra assemblies, internal NuGet packages, a second frontend or service extraction through a separate decision that identifies the consumer or operational requirement.
- Adopt structure incrementally. Move a second feature when the reference slice has passed and there is a reason to move it. Keep bulk renames, formatting-only sweeps and speculative abstractions out of behavior changes.
- A config-only setup still requires real decisions about origins, provider availability, secrets, database targeting and tenant policy. Record those inputs once per application/environment. Reuse or automate the tedious checks only after proving the setup path.

## Phase 0 inventory — 2026-11-16

**Baseline.** [PR #5](https://github.com/LlanSys/starter-template/pull/5) landed on `main` at `5a972dda2115eb7810b783b12534ec2398d657f9`. The active checkout was `codex/enterprise-saas-baseline` at PR head `8ab1954b60c69c3bd19dfd5f98d35d268f0c17f7`, aligned with its remote tracking branch. Its committed tree matched the merge commit (`git diff --name-status origin/main HEAD` was empty). At kickoff, `PLAN.md` already linked to this untracked plan, and `output/` and `tmp/` contained untracked local material. Only this plan was updated for the inventory.

- **CI:** GitHub reported successful backend build/tests, frontend build, architecture, and .NET/dependency policy checks on both the PR head and merge commit. The optional image job skipped on both. Database migration and deployment jobs skipped on the merge commit.
- **Local evidence carried forward:** [Operator acceptance](../development/operator-acceptance-results-2026-11-15.md) records password/MFA and tenant boundaries, 107 passing integration tests at that checkpoint, and real local RabbitMQ delivery. [Settings verification](../development/organization-settings-verification-2026-11-15.md) records browser save/reset and both provider checks. [Company-address verification](../development/frontend-host-routing-verification-2026-11-15.md) records two tenant sign-ins, cross-tenant rejection, browser routing, and fresh/upgrade coverage. These are separate runs against earlier working snapshots. No new runtime tests were run for the inventory.
- **Still separate:** Live Google/Microsoft sign-in plus application MFA was deferred in local acceptance. External provider and dedicated cloud runtime, real ingress, load, and recovery or rollback remain release gates. A clean renamed clone of the eventual adoption candidate belongs in phases 4 and 5.

| Owner | Data and public operations | Cross-context callers and seams |
| --- | --- | --- |
| IAM | `IamDBContext` owns accounts, roles, permissions, navigation, platform staff history, profile, credentials, challenges, sessions and refresh tokens. IAM APIs own sign-in and account/access administration. Application declares user use cases; handlers using ASP.NET Core Identity live under Infrastructure's IAM feature. | ControlPlane invitation acceptance uses `IIamUnitOfWork` and Identity to create restricted setup and assign administrator access. Platform staff and Demo use named IAM adapters that access `IamDBContext`. IAM's `TenantAccountLimitProvider` reads the ControlPlane registry for admission limits. |
| ControlPlane | `ControlPlaneDBContext` owns the tenant/stamp registry, company frontend addresses, provisioning, module catalogue and tenant grants, first-administrator invitation intent, KYC, support/impersonation and operational task history. REST serves platform operations; Deployment Stamps also exposes GraphQL over existing use cases. | Provisioning readiness checks IAM and Shared databases; invitation delivery uses the email port and its ControlPlane lease/state. KYC workers own scanning, retention and private evidence access through provider adapters. Task assignment history belongs to ControlPlane even though the Shared WorkTasks service reads IAM eligibility and projects work for users. |
| Shared | `SharedDBContext` owns organization-setting overrides and change history, payment records, email templates, failed messages and MassTransit outbox data. Shared APIs expose tenant settings, payments, dashboard and realtime endpoints. | Dashboard is a composition surface: `GetDashboardSummaryQueryHandler` reads through `IIamUnitOfWork`, and `GetDashboardAttentionQueryHandler` uses ControlPlane data. Treat these as named read exceptions and preserve tenant scope and authority. `PresentationPreferences` consumes tenant display defaults per Blazor circuit. |

**Direct solution project references** (arrows point from the consumer to its references; NuGet dependencies are outside this list):

| Project | Direct references |
| --- | --- |
| Domain; SharedKernel | None |
| SharedKernel.Validation | Domain, SharedKernel |
| Application | Domain, SharedKernel, SharedKernel.Validation |
| Persistence | Domain |
| Infrastructure | Application, Persistence |
| API | Application, Infrastructure, Persistence |
| Blazor | SharedKernel |

The API and Infrastructure composition roots configure cross-cutting services; feature workflows own their use. Each of the three contexts has its own Unit of Work and SQL Server/PostgreSQL migration history. IAM and Shared have tenant filters; the ControlPlane registry is deliberately cross-tenant and operations enforce their own permissions and target scope. Quartz uses a persistent store configured by Infrastructure; KYC and invitation jobs also have workflow state in ControlPlane, separate from Quartz and MassTransit. Profile media adapters belong to IAM and KYC evidence storage to ControlPlane. HybridCache/Redis configuration and request-scope cleanup are platform concerns; feature caches must keep their tenant/actor scope.

`BaseUnitOfWork.ExecuteInTransactionAsync` publishes in-process domain events before saving and committing its context. The retry wrapper and plain `CompleteAsync` have different dispatch behavior; Shared also exposes `CompleteWithEventsAsync`. The API registers the MassTransit bus outbox on `SharedDBContext` only. The IAM user-created event handler calls the integration publisher, which delegates to MassTransit when enabled. Existing transport tests show that Shared can persist and drain an outbox; they do not establish that an IAM write and the Shared outbox are atomic. Trace the real caller and save boundary before standardizing these paths or making a reliability claim.

| Observed gap | Phase decision |
| --- | --- |
| Architecture tests describe Persistence as allowed to depend on Application despite the current project graph; compiled checks do not forbid Application/SharedKernel from Persistence. The descriptions also understate Application's direct validation reference and Infrastructure's existing Persistence reference. | **1:** Verify direct project references and compiled dependencies separately. Keep Domain and SharedKernel backend-independent at the project boundary; enforce the Blazor HTTP boundary. Preserve server-side SharedKernel.Validation and current Identity/provider integration. |
| The legacy IAM check names removed Banking/HR contexts, while named platform dependencies cross folders and layers for real operations. | **1:** Use the observed reads, service ports and provider adapters as the starting exceptions. Prevent IAM from depending on downstream business types and prevent handlers from acquiring another context's repository as a general pattern. Cover representative permitted and forbidden cases rather than banning every cross-feature reference. |
| Blazor's settings service, dialogs and display preferences already live under the owning feature, while `AdminOrgSettings.razor` and its code-behind live under `Components/Pages/Admin`. Save/reset command, handler and validator also sit in separate buckets. | **2:** Use settings as the first organizational move. Keep the existing route, permissions, revision conflict, stable audit actor and cache invalidation. App-wide layout, session restoration, HTTP wiring and middleware remain shared. |
| The transaction helpers differ in when they dispatch, and outbox delivery evidence uses the Shared context. | **3:** Start by tracing IAM user-created dispatch through its actual admission caller(s) and Shared outbox persistence, including rollback/restart and duplicates. Keep settings history as a same-context atomicity check and first-admin setup as a resumable cross-context workflow. Split any proven behavior defect from the structure changes. |
| The earlier rename and clone walkthrough predates the adoption candidate. Source still contains the historical Quartz instance ID `BT_Q_Scheduler`. | **4–5:** Inventory effective clone identity and resources on the exact candidate. Check which identifiers the existing rename tooling changes and which persistent identities need to be preserved; do not rename a scheduler or change a key ring as part of a structural move. |

Carry forward public routes (including GraphQL), permission keys, payload and event shapes, table/schema and migration identities, scheduler identities and the Data Protection application name. No runtime or product decision changed in this phase.

## Phase 3 transaction review

The administrator-created IAM account path staged its event through a scoped publisher bound to Shared. The IAM Unit of Work committed account and profile writes, but it never saved Shared's staged event. IAM now stages its notice in the account's own transaction. The two additive IAM migrations create separate outbox tables. The existing Shared tables and worker remain available for older messages. A failed transaction clears its tracked writes so a new attempt can start cleanly.

The [persistence guide](persistence-standards.md#transaction-and-delivery-boundaries) maps the commit and delivery boundary for accounts, settings and first-administrator onboarding. It records worker ownership per physical database and the requirement for consumers to handle repeated delivery. A legacy unbound workspace database override still requires a worker pointed at that database. Runtime-provider and dedicated-stamp deployment checks remain separate.

## Phase 4 clone walkthrough — 2026-11-20

The local walkthrough used the working source at `8ab1954b60c69c3bd19dfd5f98d35d268f0c17f7` plus the adoption changes. A disposable copy was renamed to **LlanAdoption**, namespace **LA**, with its own API and Blazor secret identities and no Git remote. The code/tooling manifest has SHA256 `9BF4E2D57B67B86EC4BDCEFB336F978A08025C401C5AFF580D10A3591655F4CB`. Keep the eventual reviewed commit as the release baseline in Phase 5.

| Check | Local result |
| --- | --- |
| Rename and repeat run | Dry run preserves source. Rename updates host/project/namespace and protected-data identities. An identical rerun preserves the generated UserSecretsId values. The regression script also passes after downstream renaming. Generated output and the local environment file keep their contents. |
| Host and infrastructure allocation | The clone uses native API 6257/8229 and Blazor 6206/8149, Docker app 8280/8281, and distinct values for all infrastructure ports. Compose resolves independent project and volume names. Optional Microsoft/Google credentials are blank. |
| Endpoint audit | Focused audit accepts the clone's allocations, rejects a deliberate 8181 collision and a copied API UserSecretsId, and reports six unrelated historical project conflicts as warnings. The full audit continues to fail those workstation conflicts. |
| Setup configuration | Probes verify RabbitMQ, SMTP, scanner, Seq, SQL and Azurite port propagation, the renamed-prefix database default and an explicit database-name override. The app setup preserves configured app ports. An invalid port fails before host secret writes. These probes mocked Docker startup and user-secrets writes. |
| Package and build | Package policy and solution restore pass. Renamed API and Blazor Release builds pass. Existing analyzer warnings remain. |
| Architecture and runtime | All 70 renamed architecture tests and all 22 selected clone integration cases pass. The integration selection covers both providers' fresh/upgrade migrations, 15 real password/TOTP API sign-in cases, both providers' IAM account/outbox rollback and restart delivery, and the test-only neutral slice's authenticated access and tenant isolation. Runtime tests use disposable hosts and databases. |
| Source repository | Required API build and all 70 architecture cases pass. The focused fixture formatting check passes after applying its layout correction. |

The combined integration run exposed a fixture dependency on the process environment. The outbox fixture had scanned unrelated handlers, which failed validation when another test selected Development. It now registers its two required handlers and validates dependencies explicitly. The final combined selection passed with a saved report.

Local artifacts are under the disposable `ST-Adoption-01a1fa27e5d77a79a6bc78ec6280f1a7` directory in the Windows temporary folder. The report is `LlanAdoption/output/phase4-tests/clone-phase4-integration-final.trx`, and the source manifest is `phase4-code-manifest.sha256`. Reviewable source changes remain in the StarterTemplate working tree.

The [setup guide](../development/starter-setup.md) covers identity, ports, database selection and focused backports of later platform fixes. The historical `BT_Q_Scheduler` name is scoped by each application's database. Preserve it when upgrading an existing database. Phase 5 records the pinned source, final clean-copy acceptance and each deployed environment's provider and recovery evidence. The Alderfield browser check remains deferred by the user's instruction.

## Phase ledger

| Phase | Status | Revision and evidence | Next action |
| --- | --- | --- | --- |
| 0 Baseline and inventory | Inventory complete (local) | PR #5 merge 5a972dd; CI, source graph and local evidence recorded in the Phase 0 inventory above. | Review the documented boundaries, then begin Phase 1. |
| 1 Architecture enforcement | Verified locally | Release API and Blazor builds passed. Architecture suite passed all 69 cases, including the foreign repository regression check. Changes are in the working tree. | Begin Phase 2 with organization settings save/reset. |
| 2 Reference slice | Automated checks passed; browser deferred | API and Blazor images built and ran locally. All 70 architecture tests and eight focused integration cases passed. The Alderfield QA browser session reached MFA, where its supplied codes were rejected. The user chose to proceed to Phase 3. | Browser save/reset acceptance can be revisited independently. |
| 3 Transactions and durable work | Verified locally on configured databases | 281 unit and 70 architecture cases passed. Thirteen focused integration cases passed across the initial and corrected runs, including provider migration, account rollback and restart delivery, settings audit, and onboarding recovery. The existing real RabbitMQ transport check passed separately. | Continue to Phase 4. Track physical database overrides and deployed provider acceptance in the deployment evidence. |
| 4 Clone setup | Verified in disposable copy | LlanAdoption/LA builds, 70 architecture and 22 selected integration cases pass. Rename, port propagation, database naming and negative allocation probes pass. See the Phase 4 walkthrough above. | Prepare the reviewed, pinned Phase 5 baseline. |
| 5 Downstream release | Next | Phase 4 working-copy evidence is recorded. Release commit and final release acceptance remain open. | Review and pin the candidate, then run release acceptance. |
