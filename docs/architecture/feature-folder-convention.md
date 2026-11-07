# Feature Folder Convention

StarterTemplate retains IAM, Shared and ControlPlane. A downstream application introduces its own bounded contexts; IAM does not depend on their business entities.

## Ownership across layers

Use Features/{Context}/{Feature} consistently in Domain, Application, Infrastructure, Persistence, API and UI wherever the feature has code. Domain defines entities, invariants, events and persistence contracts. Application defines requests, validators, mappings, handlers and integration contracts. Infrastructure supplies platform/provider adapters and handlers that need external services. Persistence supplies context-specific repositories, EF configurations, seeds and migrations. API supplies permission-protected controllers and feature-owned GraphQL resolvers where a slice exposes both transports. UI calls the API through typed clients and shared DTOs.

Keep logging, middleware, generic caching, repositories, base entities, JSON utilities and other cross-cutting abstractions outside feature folders.

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

Dependency direction remains Domain <- Application <- Infrastructure/Persistence <- API. SharedKernel contains client contracts and does not reference backend layers. Blazor calls the API, not MediatR or EF directly. Each bounded context owns its Unit of Work and repositories. Write handlers call CompleteAsync with a CancellationToken. References across contexts use identifiers or stable platform contracts rather than cross-context entity navigation.

Every DbSet entity has explicit configuration. IAM and Shared queries retain tenant/soft-delete filters; ControlPlane's central registry intentionally spans tenants and is permission-protected. New contexts need their own provider migrations and tenant/audit coverage. Use separate public type files named after each type.

## Extending a clone

Define the business entity and invariants, application request/validator, bounded-context persistence contract and implementation, API permission boundary, shared DTO and typed UI client. Register that context's services without changing IAM entities or linking identity to a business record. Associate a business record with an account by its stable account identifier only when the downstream domain requires that relationship.

The test-only NeutralSlice fixture in ST.Tests.Integration proves API -> MediatR handler -> Unit of Work -> generic repository -> tenant-filtered persistence and checks 401, 403 and cross-tenant 404 outcomes. Its controller/entity/table are registered only by the test host; no demonstration business domain ships in the application.

## Names and technical identifiers

Prefer feature-specific names where necessary to avoid ambiguity, including AppUserId and TenantId. Custom persistence context types use DB (IamDBContext, DBContextHelper); framework API names remain unchanged. Use Guid.CreateVersion7 for new identifiers and System.Text.Json for contracts.
