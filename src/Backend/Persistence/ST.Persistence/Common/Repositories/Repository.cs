using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.Banking.Customers.Contracts.Repositories;
using ST.Domain.Features.HR.Employees.Contracts.Repositories;
using ST.Domain.Features.IAM.Users.Contracts.Repositories;
using ST.Domain.Shared.Contracts.Repositories;
using ST.Domain.Features.Banking.Customers.Contracts.Specifications;
using ST.Domain.Shared.Contracts.Specifications;

using ST.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Text;

namespace ST.Persistence.Common.Repositories;

public class Repository<T>(DbContext context) : IRepository<T> where T : class
{
    private readonly DbContext _context = context;

    public async Task<T> CreateAsync(T entity, CancellationToken ct = default)
    {
        await _context.Set<T>().AddAsync(entity, ct).ConfigureAwait(false);
        return entity;
    }

    public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> expression, CancellationToken ct = default)
        => await _context.Set<T>().AsNoTracking().AnyAsync(expression, ct).ConfigureAwait(false);

    public virtual async Task<int> CountAsync(CancellationToken ct = default) => await _context.Set<T>().CountAsync(ct).ConfigureAwait(false);

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>> expression, CancellationToken ct = default)
        => await _context.Set<T>().AsNoTracking().CountAsync(expression, ct).ConfigureAwait(false);

    public virtual async Task<int> CountAsync(Func<IQueryable<T>, IQueryable<T>> query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await query(_context.Set<T>().AsNoTracking()).CountAsync(ct).ConfigureAwait(false);
    }

    public virtual async Task<int> CountAsync<TCursor>(ISpecification<T, TCursor> spec, CancellationToken ct = default)
    {
        var query = _context.Set<T>().AsNoTracking();

        ArgumentNullException.ThrowIfNull(spec, nameof(spec));

        // Apply filters only — no ordering, no take, no includes
        if (spec.Criteria != null)
            query = spec.Criteria.Aggregate(query, (current, criteria) => current.Where(criteria));

        // Cursor filter is intentionally excluded — count should reflect total matching records, not records after the current page position

        return await query.CountAsync(ct).ConfigureAwait(false);
    }

    public async Task<T> DeleteAsync(Guid Id, CancellationToken ct = default)
    {
        var entity = await FindByIdAsync(Id, ct).ConfigureAwait(false);

        if (entity == null)
            throw new KeyNotFoundException($"Entity with id {Id} not found");

        _context.Set<T>().Remove(entity);
        return entity;
    }

    public async Task<T> SoftDeleteAsync(Guid Id, CancellationToken ct = default)
    {
        var entity = await FindByIdAsync(Id, ct).ConfigureAwait(false) ?? throw new KeyNotFoundException($"Entity with id {Id} not found");
        if (entity is ISoftDeletable softDeletableEntity)
        {
            softDeletableEntity.IsDeleted = true;
            _context.Set<T>().Update(entity);
        }
        else
        {
            throw new NotSupportedException($"Entity type {typeof(T).Name} does not support this method.");
        }
        return entity;
    }

    public async Task<T> DeleteAsync(string Id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(Id))
            throw new ArgumentException("Id cannot be null or empty", nameof(Id));

        if (!Guid.TryParse(Id, out var guidId))
            throw new ArgumentException("Id must be a valid GUID", nameof(Id));

        var entity = await FindByIdAsync(guidId, ct).ConfigureAwait(false);
        if (entity == null)
            throw new KeyNotFoundException($"Entity with id {Id} not found");
        _context.Set<T>().Remove(entity);
        return entity;
    }

    public async Task<T> SoftDeleteAsync(string Id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(Id))
            throw new ArgumentException("Id cannot be null or empty", nameof(Id));

        if (!Guid.TryParse(Id, out var guidId))
            throw new ArgumentException("Id must be a valid GUID", nameof(Id));

        var entity = await FindByIdAsync(guidId, ct).ConfigureAwait(false) ?? throw new KeyNotFoundException($"Entity with id {Id} not found");
        if (entity is ISoftDeletable softDeletableEntity)
        {
            softDeletableEntity.IsDeleted = true;
            _context.Set<T>().Update(entity);
        }
        else
        {
            throw new NotSupportedException($"Entity type {typeof(T).Name} does not support this method");
        }
        return entity;
    }

    public IQueryable<T> FindAll()
    {
        return _context.Set<T>().AsNoTracking();
    }

    public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression)
    {
        return _context.Set<T>().Where(expression).AsNoTracking();

    }

    public async Task<T?> FindByIdAsync(Guid Id, CancellationToken ct = default)
    {
        return await _context.Set<T>().FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == Id, ct).ConfigureAwait(false);
    }

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> expression, CancellationToken ct = default)
    {
        return await _context.Set<T>().AsNoTracking().FirstOrDefaultAsync(expression, ct).ConfigureAwait(false);
    }

    public async Task<TResult?> FirstOrDefaultAsync<TResult>(Func<IQueryable<T>, IQueryable<TResult>> query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await query(_context.Set<T>().AsNoTracking()).FirstOrDefaultAsync(ct).ConfigureAwait(false);
    }

    public async Task<List<T>> ListAsync(Func<IQueryable<T>, IQueryable<T>>? query = null, CancellationToken ct = default)
    {
        var source = _context.Set<T>().AsNoTracking();
        return await (query is null ? source : query(source)).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<List<TResult>> ListAsync<TResult>(Func<IQueryable<T>, IQueryable<TResult>> query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await query(_context.Set<T>().AsNoTracking()).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<Collection<T>> SearchAsync<TCursor>(ISpecification<T, TCursor> spec, CancellationToken ct = default)
    {
        var list = await _context.Set<T>().Specify(spec).AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
        //return new Collection<T>(list);
        return [..list];
    }

    public async Task<T> UpdateAsync(T entity, CancellationToken ct = default)
    {
        // Try to find an already-tracked instance with the same primary key and merge values
        var entityType = _context.Model.FindEntityType(typeof(T));
        var primaryKey = entityType?.FindPrimaryKey();

        if (primaryKey != null)
        {
            var keyProperties = primaryKey.Properties;

            var trackedEntry = _context.ChangeTracker
                .Entries<T>()
                .FirstOrDefault(e =>
                {
                    foreach (var pk in keyProperties)
                    {
                        var trackedVal = e.Property(pk.Name).CurrentValue;
                        var incomingVal = pk.PropertyInfo?.GetValue(entity);
                        if (!Equals(trackedVal, incomingVal))
                        {
                            return false;
                        }
                    }

                    return true;
                });

            if (trackedEntry != null)
            {
                // Merge incoming values into the tracked instance to avoid duplicate tracking
                trackedEntry.CurrentValues.SetValues(entity);
                return trackedEntry.Entity;
            }
        }

        // Fallback: safe to attach/update the incoming instance
        _context.Set<T>().Update(entity);
        return entity;
    }

    public Task<int> UpdateRangeAsync(Collection<T> entities, CancellationToken ct = default)
    {
        if (entities == null || !entities.Any())
            return Task.FromResult(0);

        _context.Set<T>().UpdateRange(entities);
        return Task.FromResult(entities.Count);
    }


}
