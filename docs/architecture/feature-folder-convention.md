# Feature Folder Convention

This solution follows a modular monolith structure where feature-owned code lives under:

```text
Features/{BoundedContext}/{Feature}
```

Use this convention consistently across Application, Domain, Persistence, and Infrastructure when code has a clear business owner. This keeps a feature easy to find in every layer without mixing unrelated concerns.

API controllers follow the same ownership rule:

```text
Api/Features/{BoundedContext}/{Feature}/Controllers
```

Use explicit bounded-context routes rather than controller-name routes:

```text
api/v{version}/{bounded-context}/{feature}
```

Examples:

- `api/v{version}/banking/customers`
- `api/v{version}/hr/employees`
- `api/v{version}/iam/users/totp`

Keep shared API base types and helpers outside feature folders, for example under `Api/Common`.

SharedKernel and SharedKernel.Validation follow the same ownership rule for externally shared contracts:

```text
SharedKernel/Features/{BoundedContext}/{Feature}/Dtos
SharedKernel.Validation/Features/{BoundedContext}/{Feature}/Validators
```

Keep only genuinely generic transport primitives in root shared folders, such as `AppResponse`, `AppRequest`, `PagedResponse`, enum converters, settings, and generic validator base types.

## Ownership Rules

- Banking owns customer business capabilities, including customer entities, customer commands/queries, customer repositories, customer email consumers, and customer number generation.
- HR owns employee business capabilities, including employee entities, employee repositories, employee events, and employee number generation.
- IAM owns identity and access capabilities, including AppUser services, claims, JWT, sessions, identity resolution, user context, and authentication-oriented SMS composition.
- Shared owns reusable platform/business-support capabilities such as notifications, email templates, lookups, outbox, failed messages, integration event publishing, caching, encryption, and background jobs.

## Feature Contracts

When an interface or implementation belongs to one feature, place it inside that feature:

```text
Features/{BoundedContext}/{Feature}/Contracts/Interfaces
Features/{BoundedContext}/{Feature}/Contracts/Implementations
```

Examples:

- `Features/Banking/Customers/Contracts/Interfaces/ICustomerNumberGenerator.cs`
- `Features/Banking/Customers/Contracts/Implementations/CustomerNumberGenerator.cs`
- `Features/IAM/Users/Contracts/Interfaces/IJwtService.cs`
- `Features/IAM/Users/Contracts/Implementations/Services/JwtService.cs`

Avoid generic feature contracts when the business meaning is specific. For example, prefer `ICustomerNumberGenerator` and `IEmployeeNumberGenerator` over a shared `INumberGenerator`.

## Feature Artifacts

Feature-owned application artifacts should live with the feature they describe:

```text
Features/{BoundedContext}/{Feature}/IntegrationEvents
Features/{BoundedContext}/{Feature}/Mappings
```

Examples:

- `Features/Banking/Customers/IntegrationEvents/CustomerCreatedIntegrationEvent.cs`
- `Features/Banking/Customers/Mappings/CustomerMapping.cs`
- `Features/HR/Employees/IntegrationEvents/EmployeeCreatedIntegrationEvent.cs`
- `Features/IAM/Users/Mappings/AppUserMapping.cs`
- `SharedKernel/Features/IAM/Users/Dtos/LoginRequest.cs`
- `SharedKernel.Validation/Features/Banking/Customers/Validators/CreateCustomerRequestValidator.cs`

Use a shared feature folder only when the artifact belongs to a shared capability rather than one business feature. For example, email template mapping belongs under `Features/Shared/EmailTemplates/Mappings`.

## Persistence Placement

Feature-specific repository interfaces belong in Domain under the owning feature, and their EF Core implementations belong in Persistence under the same feature path:

```text
Domain/Features/{BoundedContext}/{Feature}/Contracts/Repositories
Persistence/Features/{BoundedContext}/{Feature}/Repositories
```

Keep the generic EF repository base outside `Features`, for example under `Persistence/Common/Repositories`, because it is cross-cutting infrastructure.

Context-level units of work should sit at the bounded-context level, not inside one feature, because they coordinate all repositories for that context:

```text
Persistence/Features/Banking/BankingUnitOfWork.cs
Persistence/Features/IAM/IamUnitOfWork.cs
Persistence/Features/Shared/SharedUnitOfWork.cs
```

EF Core entity configurations and seed data should also live with their owning feature:

```text
Persistence/Features/{BoundedContext}/{Feature}/EntityConfigurations
Persistence/Features/{BoundedContext}/{Feature}/Seeds
```

Examples:

- `Persistence/Features/Banking/Customers/EntityConfigurations/CustomerConfiguration.cs`
- `Persistence/Features/Banking/Customers/Seeds/CustomerSeed.cs`
- `Persistence/Features/HR/Employees/EntityConfigurations/EmployeeConfiguration.cs`
- `Persistence/Features/Shared/Lookups/EntityConfigurations/CustomerStatusLookupConfiguration.cs`

Each bounded-context DbContext should apply only the configurations for its own namespace. Avoid assembly-wide configuration application without a namespace predicate, because that can accidentally pull another bounded context into the model.

## Cross-Cutting Exceptions

Do not force everything into `Features`. Keep genuinely cross-cutting code outside feature folders, preferably in existing `Common`, `Shared`, or infrastructure-level folders.

Examples:

- Generic caching abstractions and behaviors
- Logging utilities and `LoggerMessage` definitions shared by many features
- Middleware
- JSON and serialization utilities
- Base entities
- Generic repository/specification abstractions
- Transactional unit-of-work abstractions
- Generic integration event publishing

## Dependency Injection

Register feature-owned services close to their owning module/context where possible. Shared or platform services can remain in shared infrastructure registration.

Prefer explicit feature-specific registrations:

```csharp
services.AddScoped<ICustomerNumberGenerator, CustomerNumberGenerator>();
services.AddScoped<IEmployeeNumberGenerator, EmployeeNumberGenerator>();
```

Use `Lazy<T>` only when it solves a real circular dependency or expensive construction problem. It should not be the default DI pattern.

DI ownership examples:

- IAM module registers IAM services such as JWT, claims, sessions, user context, SMS composition, and AppUser services.
- Banking module registers Banking persistence and customer-owned infrastructure such as customer email composition.
- HR module registers HR persistence and employee-owned infrastructure such as employee email composition.
- Shared infrastructure registers cross-cutting services such as email delivery, SMS delivery, caching, encryption, background jobs, and integration-event publishing.

## Logging And Error Handling

Every `catch` block should log or deliberately translate the exception. Prefer existing `LoggerMessage` definitions, and add new definitions when a feature needs new structured log events.

Avoid redundant `try/catch` blocks that only rethrow. If a catch exists because the code is translating, swallowing, retrying, or enriching an exception, log through a `LoggerMessage` definition before returning or rethrowing.

## Naming Conventions

Avoid repeating the enclosing context in member names when the type or feature already supplies that context.

Examples:

- Prefer `Employee.Number` over `Employee.EmployeeNumber`.
- Prefer `EmployeeCreatedEvent.Number` over `EmployeeCreatedEvent.EmployeeNumber`.
- Prefer `Customer.Name` over `Customer.CustomerName` when the aggregate is already clearly `Customer`.

Keep the context when removing it would make the name vague or ambiguous outside that type. For example, `IEmployeeNumberGenerator` is clearer than a generic `INumberGenerator`, and foreign keys such as `AppUserId`, `EmployeeId`, or `CustomerId` should keep the referenced aggregate name.

## Collection Emptiness

Use `Any()` or `AnyAsync()` for emptiness checks. Use `Count`, `Length`, or `CountAsync()` only when the number itself is needed, such as pagination, capacity checks, or totals.

Examples:

- Prefer `items.Any()` over `items.Count > 0`.
- Prefer `!items.Any()` over `items.Count == 0`.
- Keep `items.Count > pageSize` when determining whether another page exists.

## Repository Pattern

Keep the repository/unit-of-work pattern for now. In this solution it is doing more than wrapping EF Core: it helps isolate multiple DbContexts, bounded-context units of work, specifications, domain-event dispatch, and outbox/persistence concerns.

## Commit Messages

When completing a meaningful refactor or architecture step, prepare a concise commit message that explains the intent and scope.

Use this shape:

```text
type: short outcome

Briefly mention the main architectural movement or cleanup.
```

Examples:

```text
refactor: align backend modules around bounded-context feature folders
```

```text
refactor: move IAM services into feature-owned contracts
```

Commit messages should be specific enough to understand the change later, but not a full changelog.
