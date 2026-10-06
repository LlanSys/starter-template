# StarterTemplate - Living Architecture And Delivery Plan

> Current work: [domain-neutral refactor plan](docs/architecture/domain-neutral-refactor-plan.md). Extraction settings and verification limits: [starter-extraction.md](docs/development/starter-extraction.md). BaseTemplate is the preserved business reference; inherited roadmap and readiness statements are not StarterTemplate certification.

> Last updated: 2026-07-03
>
> This file is the canonical roadmap for the StarterTemplate solution.
> It defines the architectural guardrails, execution order, and production-hardening specifications.
> Update it at the end of every meaningful work session.

---

## 1. Purpose

StarterTemplate is a reusable enterprise .NET solution template for building production-grade systems quickly, designed with a **Production-First Mindset** from the get-go.

The solution is structured as a **Logical Modular Monolith**:
- **Single Deployable Unit:** Simplifies deployment, operations, and hosting costs while the domain is evolving.
- **Bounded Contexts:** Clear separation of concerns (IAM, Workforce, Banking, Shared) with separate database schemas and DbContexts to prevent cross-context table joins.
- **Clean Architecture:** Strict inward-pointing dependency directions (Domain & SharedKernel -> Application -> Infrastructure & Persistence -> API & UI Hosts).
- **Vertical Slice Structure:** Cross-layer feature folders to maximize readability and reduce code scatteredness.
- **Client Decoupling:** Both Blazor Web and MAUI Hybrid serve as clean HTTP API clients, ensuring the web host requires zero database connectivity or secrets.

---

## 2. Architectural Direction

### 2.1 Core Architecture
The platform implements:
- Bounded Contexts with context-scoped `DbContext` and `IUnitOfWork`.
- CQRS via MediatR (templated) and Mediator.SourceGenerator (compiled).
- Domain Events (in-process) and Integration Events with MassTransit EF Outbox (cross-process).
- Architecture-tests-as-guardrails to block regression in CI/CD.

### 2.2 SaaS Tenancy And Deployment Stamps
StarterTemplate is a SaaS-ready template, not a single-client application template. It uses a hybrid deployment-stamp model:

- A tenant is a customer organization, business unit, or client workspace.
- A deployment stamp is an isolated runtime and resource boundary.
- A stamp may host one tenant or a controlled pool of tenants.
- The same container image is deployed repeatedly with different stamp-scoped configuration.
- Infrastructure must be provisioned through Bicep/Terraform-style modules, not portal copy/paste.

This means configuration, Key Vault, database, storage, cache, messaging, and CI/CD decisions must follow the selected stamp model. See `docs/architecture/saas-multitenancy-strategy.md`.

```
┌────────────────────────────────────────────────────────┐
│                      ST.UI.Rcl                         │ (Shared Razor Components)
└───────────┬────────────────────────────────┬───────────┘
            │ (HTTP REST)                    │ (HTTP REST)
┌───────────▼───────────┐        ┌───────────▼───────────┐
│     ST.UI.Blazor      │        │      ST.UI.Maui       │ (UI Hosts)
└───────────┬───────────┘        └───────────┬───────────┘
            └────────────────┬───────────────┘
                             │ (JSON over HTTP)
                    ┌────────▼────────┐
                    │     ST.Api      │ (Host Controller Gateway)
                    └────────┬────────┘
        ┌────────────────────┼────────────────────┐
┌───────▼───────┐    ┌───────▼───────┐    ┌───────▼───────┐
│ST.Infrastructure│  │ST.Persistence │    │ST.Application │ (Infrastructure / Data / Logic)
└───────┬───────┘    └───────┬───────┘    └───────┬───────┘
        │                    │                    │
        └────────────────────┼────────────────────┘
                             ▼
                     ┌───────────────┐
                     │   ST.Domain   │ (Core Entities & Business Rules)
                     └───────────────┘
```

### 2.3 Bounded Context Guardrails
To prevent logical modular monoliths from decaying into a "distributed big ball of mud," the following constraints are enforced:
1. **No Cross-DbContext Queries:** Modules can never join tables belonging to another context in SQL.
2. **References by ID Only:** Domain entities in one context (e.g., Banking `Customer`) must refer to entities in another context (e.g., IAM `AppUser`) using IDs, never navigation properties.
3. **Communication via Events:** Cross-context mutations are synchronized asynchronously using Integration Events and the MassTransit Outbox.
4. **Compile-time Assembly/Namespace Checks:** `NetArchTest` checks in `ST.Tests.Architecture` enforce that code inside a module namespace (e.g., `ST.Application.Features.Banking`) does not reference classes in another module namespace (e.g., `ST.Application.Features.HR`) directly.

### 2.4 Feature Folder Convention
Code is organized horizontally by bounded context and vertically by feature:
```text
src/Backend/Application/ST.Application/Features/{BoundedContext}/{Feature}
```
All query records, command records, validators, mapping profiles, and handlers for a given feature slice reside in the same folder. Shared plumbing (observability, caching abstractions, logging, base controllers) lives in root-level directories.

Every public top-level type must live in its own file named after that type. This applies to DTOs, validators, settings POCOs, entities, interfaces, records, enums, and public helper classes. Bundling public types into one file is not allowed because it makes future refactoring and maintenance painful.

---

## 3. Current State

### 3.1 Architecture Work Completed
- **Decoupled Frontend:** Blazor Web App (`ST.UI.Blazor`) restructured as a pure API consumer over HTTP (removes database dependencies, secrets, and MediatR references from the Web UI project).
- **Multi-Context Persistence:** Separate DbContexts (`IamDBContext`, `HrDBContext`, `BankingDBContext`, and `SharedDBContext`) created.
- **Shared Kernel & Validation:** Created `ST.SharedKernel` (DTOs) and `ST.SharedKernel.Validation` (Validators) to share validation logic between client-side Blazor/MAUI and backend MediatR behaviors.
- **IAM/Auth Enterprise Baseline:** Auth endpoints expose login, refresh, logout, current-user, permission-based authorization, server-side session tracking, sliding refresh token rotation, MFA enrollment/disable, trusted-device support, inactivity warning/logout, and profile-picture upload through a storage abstraction.
- **Audit Actor Stamping:** Persistence audit fields use the current actor provider where available and fall back to `System` only for background/startup work.
- **Profile Image Storage:** User profile pictures are supported through `IProfilePictureStorage`; local development stores files under API static assets, while Azure Blob/Azurite providers are available behind the same abstraction for cloud and local-platform hardening.
- **Provider-Based Email Delivery:** Production email is no longer SMTP-driven. Local development uses Mailpit capture, while production uses provider API mode (`SendGrid`) through typed `EmailSettings`.
- **Operational API Baseline:** OutputCache, response compression, configurable fixed-window rate limiting, SignalR hub registration, deep health-check endpoints, feature flags, QuestPDF reporting abstraction, and payment gateway abstraction with per-request NoOp/Stripe/M-Pesa provider routing are wired.
- **Architecture Tests:** Guardrails built enforcing that all Queries declare cache strategies and all Banking/HR write commands declare cache invalidation.

### 3.2 Areas That Must Still Be Completed Or Certified
- **Platform Storage Certification:** Profile image storage now supports local, Azurite, and Azure Blob providers behind `IProfilePictureStorage`; Data Protection supports local keys plus Azure Blob/Key Vault configuration. The remaining gate is local build plus Azure configuration smoke once cloud resources are ready.
- **HybridCache Certification:** Query caching and output caching are wired; complete the remaining review for native `GetOrCreateAsync` usage, negative-cache behavior, and Redis-backed multi-node invalidation.
- **MassTransit/Outbox Certification:** RabbitMQ and Azure Service Bus transports are configurable and MassTransit EF Outbox is registered. Real RabbitMQ EF-outbox-to-consumer delivery is certified by `scripts/test-local-messaging.ps1`; Azure Service Bus transport certification remains cloud-dependent.
- **Exception And Validation Coverage:** ProblemDetails and frontend parsing are in place, and the UI sanitizes backend failures before displaying them. The remaining gate is validator coverage review for command/request DTOs and tests proving backend validation messages surface cleanly in Blazor.
- **API Security And Lifecycle:** Security headers, configurable rate limiting, CORS, JWT, permission policies, API versioning, response compression, and OutputCache are present. The remaining gate is deprecation policy, throttle response consistency, CSRF stance, and operational tests.
- **Production Migrations:** Build a pipeline using `efbundle` to execute schema changes safely during deployments instead of using `db.Database.Migrate()` on startup.
- **Dynamic Navigation Maturity:** Dynamic menu/catalog management exists; continue tightening permission filtering, tenant/department scope rules, and client-facing administration workflows as feature modules grow.
- **Observability & Health Checks:** Deep health endpoints now cover self, SQL, Redis, profile-image storage, and optional Key Vault probing. Service Bus/RabbitMQ broker health remains transport-smoke dependent.
- [x] **Production Smoke Testing:** Run the full local smoke cycle against Mailpit/provider email, user-secrets, storage, cache, messaging, and Key Vault settings before promoting to Azure deployment work.
- [x] **Local Platform Certification:** Docker Compose now defines RabbitMQ, Redis, Mailpit, and Azurite, with optional SQL Server and Seq. Real RabbitMQ outbox delivery is certified; complete Redis cache behavior smoke before certifying the phase.

---

## 4. Production Hardening Conventions

For a template to be production-ready, it must enforce operational and security standards by design.

### 4.1 Caching Design System
1. **No Cache-Aside with Dummy Factories:** Always pass data-fetching delegates directly to `HybridCache` to preserve stampede protection:
   ```csharp
   // Standard Query Caching Behavior
   return await cache.GetOrCreateAsync(
       key,
       async ct => await next(ct),
       expiration,
       cancellationToken);
   ```
2. **Synchronized L1 Eviction:** Configure `HybridCache` to connect to Redis as the pub/sub backplane. When an instance removes or updates a key, it must broadcast an eviction message to clear L1 caches on other API nodes.
3. **HTTP Output Caching:** Use `AddOutputCache()` in the Web API layer for static, public endpoints (such as `/api/v1/lookups`). This bypasses controllers, DI, and DB layers completely, serving directly from cache at the routing layer.
4. **No Legacy ResponseCaching By Default:** Standardize on OutputCache for server-side API response caching. Add legacy `ResponseCaching` only if a future requirement explicitly needs HTTP header/proxy cache semantics.

### 4.2 Database Operations
1. **Zero Startup Migrations in Production:** Calling `context.Database.Migrate()` on API startup is an anti-pattern. Under load or during scaling, multiple container instances starting up concurrently will attempt to migrate the database simultaneously, causing transaction deadlocks or schema corruption.
   - *Requirement:* Build and publish database migrations via CI/CD using migration bundles (`efbundle`) executed as a release step.
2. **Tenant Isolation:** Enforce tenant scope globally. Every multi-tenant entity must implement `ITenantScoped` with a query filter on the `DbContext` bound to `CurrentTenantId`.

### 4.3 Security & Compliance
1. **Data Protection API Key Storage:** By default, ASP.NET Core stores cryptographic keys (used for cookies, session tokens, and Anti-Forgery tokens) on local disk. In a cloud environment with multiple instances, this causes random decryption errors (invalid session/CSRF tokens) during scaling.
   - *Requirement:* Configure `AddDataProtection()` to store keys in Azure Blob Storage and encrypt them using Azure Key Vault.
2. **Serilog PII Destructuring:** Guard logs against leaking sensitive data (passwords, TOTP secrets, SSNs). Use destructuring policies or ignore rules on logging configurations to strip sensitive payload properties before they hit App Insights or Seq.
3. **CSRF Enforcement:** Explicitly configure Anti-Forgery tokens on the API and Blazor hosts for all state-changing HTTP requests (POST, PUT, DELETE) using cookie authentication.

### 4.4 Observability Event Ranges
EventIds are structured by architectural layer to simplify searching in logs:

| Layer | Range | Purpose |
| --- | --- | --- |
| API | 1000-1999 | Route matching, authentication, controller execution |
| Application | 2000-2999 | CQRS Request pipelines, validation, cache triggers |
| Infrastructure | 3000-3999 | Caching engines, email/SMS gateways, vault secrets |
| Persistence | 4000-4999 | EF Core queries, migrations, Unit of Work |
| Domain | 5000-5999 | Domain events, invariants failures |

---

## 5. Capability Roadmap

```
  ┌──────────────────────────────────────────────────────────┐
  │ PHASE 0 - SaaS Tenancy & Deployment Stamp Model          │
  └──────────────────────────┬───────────────────────────────┘
                             ▼
  ┌──────────────────────────────────────────────────────────┐
  │ PHASE 1 - IAM/Auth Enterprise Baseline                   │
  └──────────────────────────┬───────────────────────────────┘
                             ▼
  ┌──────────────────────────────────────────────────────────┐
  │ PHASE 2 - Platform Storage, Cache, Validation, Messaging │
  └──────────────────────────┬───────────────────────────────┘
                             ▼
  ┌──────────────────────────────────────────────────────────┐
  │ PHASE 3 - API Security & Operational Readiness           │
  └──────────────────────────┬───────────────────────────────┘
                             ▼
  ┌──────────────────────────────────────────────────────────┐
  │ PHASE 4 - CI/CD Pipelines & Azure Deployment             │
  └──────────────────────────┬───────────────────────────────┘
                             ▼
  ┌──────────────────────────────────────────────────────────┐
  │ PHASE 5 - Template Extensibility                         │
  └──────────────────────────┴──────────────────────────┘
```

### Phase 0 - SaaS Tenancy And Deployment Stamp Model
*Goal: certify the tenancy, configuration, and deployment-stamp model before more cloud/provider work is treated as final.*
- [x] **SaaS Model:** Hybrid pooled/isolated deployment stamp strategy is documented.
- [x] **Control Plane Shape:** Define the tenant catalog fields, stamp metadata, and tenant resolution strategy.
- [x] **IaC Shape:** Define reusable stamp modules for Azure Container Apps/App Service, Key Vault, storage, database, Redis, messaging, and observability.
- [x] **Configuration Certification:** Align appsettings, POCOs, user-secrets, Key Vault names, app/ACA environment variables, and non-Azure host variables against the stamp model.
- [x] **Tenant Isolation Tests:** Add tests proving tenant-scoped queries and writes cannot cross tenant boundaries.

### Phase 1 - IAM/Auth Enterprise Baseline
*Goal: certify identity, authentication, authorization, MFA, sessions, and local UI/API smoke testing.*
- [x] **IAM/Auth Baseline:** Login, refresh, logout, server-side sessions, lockout, TOTP MFA, admin MFA enforcement, grant/revoke access, current-user, profile picture upload, and inactivity warning are implemented.
- [x] **Local Smoke Certification:** Complete browser/email smoke testing for login, MFA challenge/setup/disable, grant access email, revoke access, refresh, logout, and session timeout.

### Phase 2 - Platform Storage, Cache, Validation, Messaging
*Goal: harden shared platform services before deployment work.*
- [~] **Harden HybridCacheService:** Query caching is wired; certify `GetOrCreateAsync` behavior and remove any remaining dummy/negative cache paths that can pollute cache state.
- [~] **Enable Redis L1 Eviction Bus:** Redis distributed cache and `IConnectionMultiplexer` registration exist; certify multi-node invalidation behavior explicitly.
- [x] **Add Namespace Dependency Tests:** NetArchTest blocks cross-bounded-context namespace references.
- [x] **Configure Output Caching:** `AddOutputCache`, `UseOutputCache`, and lookup endpoint policies are registered.
- [x] **Response Compression:** ASP.NET Core response compression is registered and controlled by `ResponseCompression` settings.
- [x] **Provider-Based Email Delivery:** Local Mailpit and production SendGrid API modes are registered; personal mailbox SMTP is not a production delivery path.
- [~] **Profile Media Storage:** Local and Azure Blob profile picture providers exist behind `IProfilePictureStorage`; certify Azure settings with real storage.
- [x] **Validation Coverage:** Shared DTO validators and Application write-command validators are being expanded for admin/customer/employee/reference flows.
- [x] **MassTransit/Outbox:** RabbitMQ/Azure Service Bus switching and EF outbox are wired; RabbitMQ publish/consume and health behavior are certified locally, while Azure Service Bus certification remains cloud-dependent.

### Phase 3 - API Security And Operational Readiness
*Goal: Ensure the system can be monitored, scaled, and diagnosed under load.*
- [x] **Deep Health Checks:** Active endpoints cover self, SQL, Redis, profile-image storage, and optional Key Vault secret probing. Broker-specific transport smoke remains separate.
- [x] **Configure ASP.NET Data Protection:** Local file/DPAPI and Azure Blob/Key Vault settings are wired and certified with Azure resources.
- [x] **PII Log Sanitization:** Implement Serilog destructuring policies to mask passwords, MFA codes, and personal identifiers.
- [x] **Correlated Tracing:** OpenTelemetry/Azure Monitor wiring exists; certify trace context propagation across Blazor client HTTP requests -> API gateways -> MediatR pipelines -> EF Core.
- [x] **API Lifecycle:** Define deprecation headers, throttle response consistency, and API security policy tests.

### Phase 4 - CI/CD Pipelines And Deployment
*Goal: establish secure, repeatable deployment guardrails without manual steps, while supporting Azure, non-Azure container hosts, and local Docker smoke deployments.*
- [x] **efbundle Migration Pipeline:** GitHub Actions compiles context-specific EF migration bundles and executes them through OIDC with Azure SQL wake-up retries and temporary runner firewall access. This is verified with a successful Azure migration.
- [x] **Docker Local Platform:** Docker Compose defines RabbitMQ, Redis, Mailpit, Azurite, optional SQL Server, and optional Seq. Add app-host Dockerfiles and local app-compose smoke deployment before marking deployment-local complete.
- [x] **Container Publish Workflow:** API/UI Dockerfiles and GHCR build/publish workflow are added and successfully verified with image pull deployments.
- [x] **Deployment Target Matrix:** Support documented deployment targets: Azure App Service/Container Apps, DigitalOcean/Heroku/generic Docker, and local app-compose.
- [x] **Azure Deployment Workflows:** Wire Blue/Green deployment slot switches for API services in GitHub Actions after Azure subscription/billing is active.

### Phase 5 - Template Extensibility
*Goal: complete reusable extension points without adding product-specific SACCO/domain features to the template.*
- [x] **Dynamic Permissions-Based Menus:** Implement backend menu/module API that constructs the UI layout based on user permissions, roles, and active feature flags.
- [x] **MudBlazor UI Shell:** Implement responsive theme, layouts, and auth pages in the Shared Razor Class Library (RCL).
- [x] **Feature Flags:** Generic fail-closed feature-flag abstraction is registered with configuration-backed evaluation.
- [x] **SignalR Baseline:** Authenticated notification hub structure and tenant grouping convention are registered.
- [x] **Reporting Baseline:** QuestPDF reporting abstraction is registered without product-specific reports.
- [x] **Payment Gateway Integrations:** `IPaymentGateway` abstraction plus NoOp, Stripe Checkout, M-Pesa STK/query adapters, and per-request provider routing are registered. Remaining certification is real-provider credentials, callback smoke tests, idempotency, reconciliation, and provider-specific operational runbooks.
- [x] **Entra ID SSO:** OIDC scheme and typed configuration are registered. AppUser linking/token issuance callback flow and UI entry point are implemented.
- [x] **Passkeys/WebAuthn:** Add passkey registration/authentication ceremonies, credential storage, browser challenge flow, and recovery policy.

---

## 6. Open Decisions

- **Cookie vs. Hybrid Auth in Blazor:** Should Blazor use pure cookie authentication with backend session matching or access tokens (JWT) stored client-side?
  - *Prod Context:* Cookies are more secure against XSS, but require a reverse proxy or same-site routing. JWTs allow client-side independence.
- **Audit Logging Model:** Should we use EF Core interceptors mapping changes to a central `AuditLogs` table, or push audit events to an asynchronous message broker for external archiving?
- **Module Physical Split Trigger:** At what team or codebase size should folders in `ST.Domain` / `ST.Application` be physically split into individual project assemblies?

---

## 7. Session Onboarding

**Start of Session:**
1. Review this document.
2. Ensure Docker containers for SQL, Redis, and Seq are running locally.
3. Run `tests/checks.ps1` or run tests locally via Visual Studio to ensure base health.

**End of Session:**
1. Update this roadmap to check off completed capabilities.
2. Run architecture tests to confirm no structural boundaries were broken.
3. Commit using descriptive, outcome-focused messages.

## StarterTemplate direction approved 2026-10-06

The user selected a new StarterTemplate repository, preserving StarterTemplate as the business-domain reference. The implementation inventory and phase gates are in [domain-neutral-refactor-plan.md](docs/architecture/domain-neutral-refactor-plan.md). Customer-reference gates below describe the existing reference application; they must not require Banking/HR retention in StarterTemplate. Preserve the current architecture and platform infrastructure. Create the new repository from a reviewed baseline before performing business removal.
