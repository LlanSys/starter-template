using ST.Domain.Shared.Contracts.Specifications;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Persistence.Extensions;

public static class SpecificationExtensions
{
    public static IQueryable<T> Specify<T, TCursor>(this IQueryable<T> inputQuery, ISpecification<T, TCursor> spec) where T : class
    {
        ArgumentNullException.ThrowIfNull(inputQuery);
        ArgumentNullException.ThrowIfNull(spec);

        return SpecificationEvaluator<T, TCursor>.GetQuery(inputQuery, spec);
    }
}
