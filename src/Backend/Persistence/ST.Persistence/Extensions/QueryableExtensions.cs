using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Persistence.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> InDisplayOrder<T>(this IQueryable<T> query) where T : class, IOrderable
        => query.OrderBy(x => x.DisplayOrder);



}
