# Feature Folder Convention

StarterTemplate retains IAM, Shared and ControlPlane. A downstream application introduces its own bounded contexts; IAM does not depend on their business entities.

## Ownership across layers

Use Features/{Context}/{Feature} consistently in Domain, Application, Infrastructure, Persistence, API and UI wherever the feature has code. Domain defines entities, invariants, events and persistence contracts. Application defines requests, validators, mappings, handlers and integration contracts. Infrastructure supplies platform/provider adapters and handlers that need external services. Persistence supplies context-specific repositories, EF configurations, seeds and migrations. API supplies permission-protected controllers and feature-owned GraphQL resolvers where a slice exposes both transports. UI calls the API through typed clients and shared DTOs.

Keep feature pages, their adjacent code and typed UI services under the owning feature. Use the host composition root for service registration and shared locations for middleware, the Blazor shell and session restoration, generic repositories, logging, caching, base entities, and JSON utilities. Provider selection is composed at the host while a feature's storage or external-service adapter stays with the feature.

## Retained contexts

- IAM owns accounts, contact profiles, roles, permissions, menus, authentication, MFA, passkeys, sessions and refresh tokens. Accounts have no Employee, Customer or Department dependency.
- Shared owns provider-neutral payments, notification/email infrastructure, reporting, storage, outbox and shared platform capabilities. Payment CustomerReference is a correlation identifier and remains supported.
- ControlPlane owns tenant registration, administrative approval, deployment stamps, provisioning, suspension and tenant modules. Product-specific compliance policy belongs in the downstream application.

## Concrete layout

```text
Application/Features/IAM/Users/Commands/CreateAppUserCommand.cs
Infrastructure/Features/IAM/AspNetCoreIdentity/CommandHandlers/CreateAppUser.cs
Domain/Features/IAM/Users/Entities/AppUser.cs
Domain/Features/IAM/Contracts/IIamUnitOfWork.cs
Persistence/Features/IAM/IamUnitOfWork.cs
Persistence/Features/IAM/Users/EntityConfigurations/AppUserConfiguration.cs
Persistence/Features/IAM/Users/Repositories/IamAppUserProfileRepository.cs
Persistence/Features/IAM/Migrations/SqlServer/
Persistence/Features/IAM/Migrations/PostgreSql/
Api/Features/IAM/Users/Controllers/IamAdminController.cs
SharedKernel/Features/IAM/Users/Dtos/CreateAppUserRequest.cs
SharedKernel.Validation/Features/IAM/Users/Validators/CreateAppUserRequestValidator.cs
UI.Blazor/Features/IAM/Users/Pages/AdminUsers.razor
```

## Dependency and persistence rules

| Project | Direct solution project references |
| --- | --- |
| Domain, SharedKernel | None |
| SharedKernel.Validation | Domain, SharedKernel |
| Application | Domain, SharedKernel, SharedKernel.Validation |
| Persistence | Domain |
| Infrastructure | Application, Persistence |
| API | Application, Infrastructure, Persistence |
| Blazor | SharedKernel |

The architecture suite checks declared project references and compiled assembly references separately. Infrastructure and API also use Domain and SharedKernel types exposed through their dependencies. SharedKernel.Validation is a server-side dependency of Application. Blazor uses typed HTTP clients and SharedKernel DTOs. Its boundary excludes compiled MediatR and EF Core dependencies.

Each bounded context owns its Unit of Work and repositories. Write handlers call `CompleteAsync` with a `CancellationToken`. Use stable identifiers, published events, or a named application operation or projection for cross-context work. Product features use IAM through stable contracts. IAM does not depend on product types, and product contexts do not use IAM entities, Infrastructure implementations, or Persistence.

The architecture suite checks compiled use of other contexts' repository contracts, units of work, and Persistence types in Application and Infrastructure feature workflows. The following existing callers have specific exceptions. The [allowlists](../../tests/ST.Tests.Architecture/ContextDependencyTests.cs) name the individual types and the owning context.

| Consumer | Dependency and purpose |
| --- | --- |
| Shared dashboard and operational tasks | Dashboard summary reads IAM, attention reads ControlPlane, and task assignment records ControlPlane work and checks IAM eligibility. |
| ControlPlane onboarding, managed support, Demo, staff, and module operations | IAM access supports staged administrator onboarding, scoped workspaces and proxy sessions, platform staff management, and role or entitlement operations. Provisioning readiness checks the IAM and Shared database targets. |
| IAM session, invitation, and account operations | ControlPlane access checks tenant lifecycle, support grants and session targets, and account admission limits. |

Every DbSet entity has explicit configuration. IAM and Shared queries retain tenant/soft-delete filters; ControlPlane's central registry intentionally spans tenants and is permission-protected. New contexts need their own provider migrations and tenant/audit coverage. Use separate public type files named after each type.

## Extending a clone

Define the business entity and invariants, application request/validator, bounded-context persistence contract and implementation, API permission boundary, shared DTO and typed UI client. Register that context's services without changing IAM entities or linking identity to a business record. Associate a business record with an account by its stable account identifier only when the downstream domain requires that relationship.

The test-only NeutralSlice fixture in ST.Tests.Integration proves API -> MediatR handler -> Unit of Work -> generic repository -> tenant-filtered persistence and checks 401, 403 and cross-tenant 404 outcomes. Its controller/entity/table are registered only by the test host; no demonstration business domain ships in the application.

Organization settings save/reset traces the production path from a feature-owned page through the typed client and permission-protected API to a co-located application use case and tenant-scoped persistence. See the [file map and downstream recipe](../development/organization-settings.md#save-and-reset-reference-slice).

## Names and technical identifiers

Prefer feature-specific names where necessary to avoid ambiguity, including AppUserId and TenantId. Custom persistence context types use DB (IamDBContext, DBContextHelper); framework API names remain unchanged. Use Guid.CreateVersion7 for new identifiers and System.Text.Json for contracts.
