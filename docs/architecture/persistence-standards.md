# Persistence Standards

> Last updated: 2026-06-01
>
> This document defines how StarterTemplate uses EF Core, repositories, Unit of Work, soft delete, and feature-bound persistence code.

---

## 1. Decision

StarterTemplate keeps the repository and Unit of Work pattern on top of EF Core.

EF Core already implements many repository-like and unit-of-work mechanics through `DbSet<T>` and `DbContext`, but the template still uses explicit repositories because they give us:

- A stable persistence boundary per bounded context.
- A consistent place for soft delete, specifications, paging, and query conventions.
- A domain/application-facing abstraction that avoids leaking EF Core everywhere.
- Cleaner feature slices: Application depends on feature contracts, Persistence provides implementations.
- A future seam if a feature needs read models, stored procedures, external stores, or specialized persistence.

The rule is not "create custom repository methods for everything." The rule is "use the generic repository by default, and add feature-specific repository behavior only when it earns its place."

Custom persistence context-related types use the project `DB` acronym. Use names such as `IamDBContext`, `IamDBContextFactory`, `DBContextHelper`, and `ITenantFilteredDBContext`. Keep Microsoft framework API names unchanged, for example `DbContext`, `DbSet<T>`, `AddDbContext`, and `IDesignTimeDbContextFactory`.

When creating `IDesignTimeDbContextFactory<T>` implementations for specific providers, always specify a provider-specific fallback connection string (e.g., `DesignTimeConfigurationFactory.GetConnectionString(..., fallbackConnectionName: "DefaultPostgreSqlConnection")`) so that the factory doesn't crash reading an incompatible global connection string (like `Encrypt=True` for Postgres).

---

## 2. Generic Repository Responsibilities

`IRepository<T>` and `Repository<T>` own common persistence operations:

- Create.
- Update.
- Hard delete where explicitly intended.
- Soft delete for entities implementing `ISoftDeletable`.
- Find by id.
- Find by condition.
- Count.
- Specification-based search.
- Batch update where needed.

If a feature operation can be handled cleanly by these methods plus a specification/query handler, do not duplicate it in the concrete repository.

---

## 3. Concrete Repository Responsibilities

Concrete repositories should be feature-bound and usually thin:

```csharp
internal sealed class IamMenuRepository(IamDBContext context)
    : Repository<MenuItem>(context), IMenuRepository
{
}
```

This is the preferred shape for normal aggregate repositories such as Menus, Permissions, Email Templates, and Failed Messages.

Concrete repositories may add methods only for persistence concerns that the generic repository cannot express cleanly.

Allowed reasons include:

- Aggregate-specific queries with required includes, ordering, or filters.
- Security/session/token lifecycle queries.
- Cross-table routing where the entity type is selected at runtime.
- Bulk operations that must be executed consistently within a bounded context.
- Persistence-specific cleanup or maintenance operations.
- Queries that would otherwise force EF Core details into Application handlers.

Not allowed:

- Repeating generic `Create`, `Update`, `Delete`, `FindById`, or `Search` methods per entity.
- Moving business orchestration into repositories.
- Calling repositories from controllers directly.
- Injecting `DbContext` into Application handlers.
- Creating a concrete repository just to rename generic operations.

---

## 4. Unit Of Work Boundary

The Unit of Work should coordinate `SaveChangesAsync`.

Repositories should normally stage changes only. They should not call `SaveChangesAsync` inside ordinary create, update, revoke, or delete methods unless the method is intentionally an atomic persistence operation and the exception is documented.

Preferred flow:

1. Application command handler calls repository methods.
2. Repository stages changes on the correct context.
3. Unit of Work commits once.
4. Transaction/pipeline behavior owns cross-operation consistency where applicable.

This keeps command handlers predictable and prevents partial commits inside a larger workflow.

---

## 5. Query And Search Pattern

For list screens and searchable APIs:

- Prefer request DTOs for search/filter/page inputs.
- Prefer specification objects or focused query handlers for reusable query rules.
- Keep UI services thin and API-backed.
- Keep filter names business-friendly, not database-shaped.
- Use `Any` for existence checks rather than `Count > 0`.
- Compose on `IQueryable` until tenant, security, status, search, ordering, paging, and projection have all been applied.
- Do not call `ToListAsync` before applying filters, role/user scope, paging, or projection unless the result is intentionally bounded and documented.
- Apply user-specific filters such as `UserId`, `TenantId`, role, status, and expiry in the database query, not after materialization.
- Use `CountAsync` only when the count is returned, logged, or used for a decision; never run count queries and discard the result.
- For cursor pagination, fetch `pageSize + 1` rows so the API can reliably determine whether a next page exists.
- Avoid N+1 query patterns. Do not run repository, `UserManager`, or lookup calls per row in a result loop; batch identifiers and map results in memory.
- Use `AsNoTracking` for read-only queries and tracked queries only when the entity will be mutated in the same unit of work.

The account administration list is the current UI/API reference flow for search, filters, pagination, table actions, dialogs, and confirmation flows.

---

## 6. Audit Actor Standard

Audit fields must capture a stable actor identifier, not a display name or free-text label.

- Use the authenticated user id for user-driven changes.
- Use an explicit system actor id only for bootstrap, background, or startup work where no human user exists.
- Do not write labels such as `DevelopmentSeed` into `CreatedBy`, `UpdatedBy`, `ActivatedBy`, `DeactivatedBy`, or `DeletedBy`.
- Creation fields are normally immutable after insert; development fixture seeders may normalize bad historical seed values.
- Access lifecycle methods must stamp both lifecycle fields and update fields. For example, grant access sets `ActivatedAt`, `ActivatedBy`, `UpdatedAt`, and `UpdatedBy`, and clears stale deactivation metadata.
- Revoke/deactivate flows must set `DeactivatedAt`, `DeactivatedBy`, `DeactivationReason`, `UpdatedAt`, and `UpdatedBy`.

---

## 7. Soft Delete Standard

Entities that represent business data or audit-relevant records should support soft delete unless there is a clear reason not to.

Soft-deleteable entities should:

- Implement `ISoftDeletable`.
- Have EF query filters excluding deleted records by default.
- Use repository `SoftDeleteAsync` or aggregate methods such as `MarkAsDeleted`.
- Preserve hard delete only for explicit purge/retention scenarios.

Delete actions exposed to users should normally be soft deletes.

---

## 8. Accepted Exceptions

Some repositories are expected to be less thin because their persistence behavior is not generic.

Current accepted examples:

- `SharedLookupRepository`: routes a runtime `LookupType` to separate lookup tables and creates the correct concrete lookup entity.
- IAM token/session/TOTP repositories: contain security lifecycle queries such as active tokens, active sessions, expired secrets, revocation, and cleanup.
- Profile upsert repository methods: centralize persistence-specific "create or update by user id" behavior.

Even for accepted exceptions, the repository should still avoid unnecessary `SaveChangesAsync` calls and avoid business orchestration.

---

## 9. Review Checklist

When adding or reviewing a repository, check:

- Does the feature already get what it needs from `IRepository<T>`?
- Is this concrete repository method truly persistence-specific?
- Could this be a specification/query handler instead?
- Does it avoid duplicating generic CRUD?
- Does it avoid business orchestration?
- Does it respect soft delete and query filters?
- Does it avoid committing changes before the Unit of Work?
- Does it avoid materializing before filtering, paging, or projection?
- Does it avoid N+1 calls inside result loops?
- Does every cursor-paged query fetch `pageSize + 1` before trimming the response?
- Is the repository placed under the correct feature folder?

If the answer is unclear, start with the generic repository and add a concrete method only when the use case proves it needs one.

### PostgreSQL JSONB
When using PostgreSQL as the primary database provider, you can safely utilize jsonb columns for unstructured or highly dynamic data (e.g., audit trails, untyped webhook payloads, or form submissions) when appropriate. PostgreSQL handles jsonb extremely efficiently and allows GIN indexing on these columns, providing excellent performance for complex queries. Ensure that business-critical relational data remains strongly typed.

## Fresh starter baseline and concurrency

Only IAM, Shared and ControlPlane ship in the starter. Generate separate SQL Server and PostgreSQL migrations in Features/{Context}/Migrations/{SqlServer|PostgreSql}; each context has its own migration history. InitialStarter targets an empty database. Existing downstream database upgrades require a separate migration strategy.

SQL Server uses native rowversion. PostgreSQL stores a required byte concurrency token and assigns a new cryptographically random value on every added/modified row through DBContextHelper. EF includes the original value in update predicates; conflicting writers fail rather than overwrite silently. ControlPlane applies this convention even though its registry is deliberately not tenant-filtered.

PasskeyChallenge is an explicit exception to soft delete. It is transient, tenant-scoped ceremony state with five-minute expiry. Atomic conditional deletion gives one consumer ownership; failed and malformed attempts consume the challenge. Do not replace this with a distributed-cache read/remove pair.

Shared's initial baseline also installs the provider-specific Quartz schema without the historical reset/drop statements. EF's context history protects repeat execution. Quartz reads DefaultConnection, which must point to the database containing those tables.
