using ST.Domain.Shared.Contracts.Specifications;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;

namespace ST.Persistence.Extensions;

internal static class SpecificationEvaluator<T, TCursor> where T : class
{
    public static IQueryable<T> GetQuery(IQueryable<T> inputQuery, ISpecification<T, TCursor> spec)
    {
        var query = inputQuery;

        // Apply Tracking and Query Splitting settings from Spec
        //if (spec.AsNoTracking) query = query.AsNoTracking(); - Already done in Repository SearchAsync
        if (spec.UseSplitQuery) query = query.AsSplitQuery();

        // Filters
        if (spec.Criteria != null)
            query = spec.Criteria.Aggregate(query, (current, criteria) => current.Where(criteria));

        // Robust Cursor Paging (Merged from CursorPredicateBuilder)
        if (spec.CursorFilter != null)
        {
            query = query.Where(spec.CursorFilter);
        }
        else if (spec.Cursor != null && spec.CursorProperty != null)
        {
            var predicate = BuildCursorPredicate(spec.Cursor, spec.CursorProperty);
            if (predicate != null) query = query.Where(predicate);
        }

        // Includes
        query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));

        // Ordering
        if (spec.OrderBy != null) query = query.OrderBy(spec.OrderBy);
        else if (spec.OrderByDescending != null) query = query.OrderByDescending(spec.OrderByDescending);

        // Paging
        return query.Take(spec.Take + 1);
    }

    private static Expression<Func<T, bool>>? BuildCursorPredicate(TCursor cursorValue, string propertyName)
    {
        var param = Expression.Parameter(typeof(T), "x");
            var propExpr = Expression.PropertyOrField(param, propertyName);
            var underlyingType = Nullable.GetUnderlyingType(propExpr.Type) ?? propExpr.Type;

            // Type-safe conversion
            var convertedValue = underlyingType switch
            {
                var t when t == typeof(Guid) => cursorValue is Guid g
                    ? g
                    : Guid.TryParse(cursorValue?.ToString(), out var parsedGuid)
                        ? parsedGuid
                        : throw new InvalidOperationException($"Cannot convert cursor value '{cursorValue}' to Guid"),

                var t when t.IsEnum => Enum.TryParse(t, cursorValue?.ToString(), ignoreCase: true, out var parsedEnum)
                    ? parsedEnum
                    : throw new InvalidOperationException($"Cannot convert cursor value '{cursorValue}' to enum {t.Name}"),

                _ => Convert.ChangeType(cursorValue, underlyingType, CultureInfo.InvariantCulture)
                    ?? throw new InvalidOperationException($"Cannot convert cursor value '{cursorValue}' to {underlyingType.Name}")
            };

            var constant = Expression.Constant(convertedValue, underlyingType);

            // Build comparison using CompareTo for strings/Guids/numbers
            var compareToMethod = underlyingType.GetMethod("CompareTo", [underlyingType])
                ?? throw new InvalidOperationException($"Type {underlyingType.Name} does not support CompareTo — cannot build cursor predicate for property '{propertyName}'");

            // Handle Nullable types by accessing .Value
            Expression left = propExpr.Type != underlyingType
                ? Expression.Property(propExpr, "Value")
                : propExpr;

            var call = Expression.Call(left, compareToMethod, constant);
            var comparison = Expression.GreaterThan(call, Expression.Constant(0));

            // If nullable, add the .HasValue check
            if (Nullable.GetUnderlyingType(propExpr.Type) != null)
            {
                var hasValue = Expression.Property(propExpr, "HasValue");
                return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(hasValue, comparison), param);
            }

        return Expression.Lambda<Func<T, bool>>(comparison, param);
    }

}
