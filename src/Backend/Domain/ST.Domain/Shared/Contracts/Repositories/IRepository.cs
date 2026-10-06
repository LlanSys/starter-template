using ST.Domain.Features.Banking.Customers.Contracts.Specifications;
using ST.Domain.Shared.Contracts.Specifications;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Text;

namespace ST.Domain.Shared.Contracts.Repositories;

public interface IRepository<T> where T : class
{
    Task<T> CreateAsync(T entity, CancellationToken ct = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> expression, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<T, bool>> expression, CancellationToken ct = default);
    Task<int> CountAsync(Func<IQueryable<T>, IQueryable<T>> query, CancellationToken ct = default);
    Task<int> CountAsync<TCursor>(ISpecification<T, TCursor> spec, CancellationToken ct = default);
    Task<T> DeleteAsync(Guid Id, CancellationToken ct = default);
    Task<T> SoftDeleteAsync(Guid Id, CancellationToken ct = default);
    Task<T> DeleteAsync(string Id, CancellationToken ct = default);
    Task<T> SoftDeleteAsync(string Id, CancellationToken ct = default);
    IQueryable<T> FindAll();
    Task<T?> FindByIdAsync(Guid Id, CancellationToken ct = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> expression, CancellationToken ct = default);
    Task<TResult?> FirstOrDefaultAsync<TResult>(Func<IQueryable<T>, IQueryable<TResult>> query, CancellationToken ct = default);
    IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression);
    Task<List<T>> ListAsync(Func<IQueryable<T>, IQueryable<T>>? query = null, CancellationToken ct = default);
    Task<List<TResult>> ListAsync<TResult>(Func<IQueryable<T>, IQueryable<TResult>> query, CancellationToken ct = default);
    Task<Collection<T>> SearchAsync<TCursor>(ISpecification<T, TCursor> spec, CancellationToken ct = default);
    Task<T> UpdateAsync(T entity, CancellationToken ct = default);
    Task<int> UpdateRangeAsync(Collection<T> entities, CancellationToken ct = default);

}
