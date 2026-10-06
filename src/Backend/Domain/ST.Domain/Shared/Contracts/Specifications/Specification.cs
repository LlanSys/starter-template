using ST.Domain.Shared.Contracts.Specifications;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Text;

namespace ST.Domain.Shared.Contracts.Specifications;

public class Specification<T, TCursor> : ISpecification<T, TCursor>
{
    public Collection<Expression<Func<T, bool>>> Criteria { get; } = [];
    public Expression<Func<T, bool>>? CursorFilter { get; private set; }
    public Collection<Expression<Func<T, object>>> Includes { get; } = [];
    public Collection<string> IncludeStrings { get; } = [];
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public TCursor? Cursor { get; private set; }
    public string? CursorProperty { get; private set; }
    public int Take { get; private set; } = 50;
    public bool UseSplitQuery { get; private set; }
    public bool AsNoTracking { get; private set; } = true;


    protected void AddCriteria(Expression<Func<T, bool>> criteria)
    {
        Criteria.Add(criteria);
    }

    /// <summary>
    /// Sets both the cursor value (returned as NextCursor to client)
    /// and the cursor comparison expression applied to the query.
    /// </summary>
    protected void SetCursor(TCursor? cursor, Expression<Func<T, bool>>? cursorFilter)
    {
        Cursor = cursor;
        CursorFilter = cursorFilter;
    }
    protected void SetCursor(TCursor? cursor, string cursorProperty = "Id")
    {
        Cursor = cursor;
        CursorProperty = cursorProperty;
    }

    protected void AddInclude(Expression<Func<T, object>> include)
    {
        Includes.Add(include);
    }

    protected void AddOrderBy(Expression<Func<T, object>> orderBy)
    {
        OrderBy = orderBy;
    }

    protected void AddOrderByDescending(Expression<Func<T, object>> orderByDescending)
    {
        OrderByDescending = orderByDescending;
    }

    protected void SetTake(int take)
        => Take = take;

    protected void EnableSplitQuery()
        => UseSplitQuery = true;

    protected void DisableNoTracking()
        => AsNoTracking = false;

}
