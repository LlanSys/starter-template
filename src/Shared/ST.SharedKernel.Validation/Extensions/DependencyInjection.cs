using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using FluentValidation;
using System.Text;

namespace ST.SharedKernel.Validation.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedValidationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
