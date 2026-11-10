using System.Diagnostics;
using HotChocolate;
using HotChocolate.Execution;

namespace ST.Api.Common.GraphQL;

internal sealed class TraceIdErrorFilter : IErrorFilter
{
    public IError OnError(IError error)
    {
        var traceId = Activity.Current?.Id;
        return traceId is null ? error : error.SetExtension("traceId", traceId);
    }
}
