using MassTransit;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Infrastructure.Extensions;

public static class ConsumeContextExtensions
{
    public static int GetRetryCount(this ConsumeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Headers.TryGetHeader("MT-Redelivery-Count", out var value)
            && value is not null
            && int.TryParse(value.ToString(), out var count)
            ? count
            : 0;
    }
}