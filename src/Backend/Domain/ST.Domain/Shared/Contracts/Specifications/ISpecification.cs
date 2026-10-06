using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Text;

namespace ST.Domain.Shared.Contracts.Specifications;

public interface ISpecification<T, TCursor>
{
    Collection<Expression<Func<T, bool>>>? Criteria { get; }

    /// <summary>
    /// Cursor comparison expression — e.g. x => x.Id > cursor.
    /// Built by the spec itself. Null when fetching first page.
    /// </summary>
    Expression<Func<T, bool>>? CursorFilter { get; }
    Collection<Expression<Func<T, object>>> Includes { get; }
    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    /// The raw cursor value — returned to the client as NextCursor.
    /// Used by the repo to return the next cursor after fetching.
    /// </summary>
    TCursor? Cursor { get; }

    /// <summary>
    /// Name of the property to apply cursor comparison against.
    /// e.g. "Id", "CreatedAt".
    /// Persistence uses this to build the predicate via CursorPredicateBuilder.
    /// </summary>
    string? CursorProperty { get; }

    /// <summary>How many records to fetch. Fetch Take+1 to determine HasMore.</summary>
    int Take { get; }
    bool UseSplitQuery { get; }
    bool AsNoTracking { get; }


}
