using ST.Application.Behaviours;
using ST.Application.Configuration;
using ST.Application.Contracts.Implementations.Common;
using ST.Application.Contracts.Interfaces.Common;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ST.Application.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        RegisterApplicationServices(services, configuration);
        return services;
    }

    private static void RegisterApplicationServices(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        services.Configure<MediatRSettings>(configuration.GetSection(MediatRSettings.SectionName));

        var mediatrSettings = configuration.GetSection(MediatRSettings.SectionName).Get<MediatRSettings>() ?? new MediatRSettings();
        var licenseKey = mediatrSettings.LicenseKey;
        if (string.IsNullOrWhiteSpace(licenseKey))
        {
            licenseKey = "your-community-license-key-here";
        }

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.LicenseKey = licenseKey;
            cfg.AddOpenBehavior(typeof(ExceptionHandlingBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehaviour<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
            cfg.AddOpenBehavior(typeof(CacheInvalidationBehavior<,>));

        });

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<IServiceManager, ServiceManager>();

    }
}
