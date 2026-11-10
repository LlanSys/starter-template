using ST.Domain.Features.IAM.Users.Entities;
using ST.Infrastructure.Configuration;
using ST.Infrastructure.Features.IAM.Users.Contracts.Implementations.Services;
using ST.Infrastructure.Features.Shared.Notifications.Contracts.Implementations.Services;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Application.Features.Shared.Notifications.Contracts.Interfaces;
using ST.Infrastructure.Extensions;
using ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;
using ST.Infrastructure.Features.IAM.Users.Seeding;
using ST.Infrastructure.Features.IAM.Authorization;
using ST.Persistence.Features.IAM.DataContext;
using ST.Persistence.Features.IAM.Extensions;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ST.Infrastructure.Features.IAM.Extensions;

public static class IamModuleDI
{
    public static IServiceCollection AddIamModule(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddIamPersistence(configuration, environment);

        services.Configure<DevelopmentSeedSettings>(configuration.GetSection(DevelopmentSeedSettings.SectionName));

        services.AddIdentity<AppUser, AppRole>(DependencyInjection.ConfigureIdentityOptions)
            .AddEntityFrameworkStores<IamDBContext>()
            .AddDefaultTokenProviders();

        DependencyInjection.ConfigureAuthentication(services, configuration);

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Login>());

        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IClaimsService, ClaimsService>();
        services.AddScoped<IamDelegation>();
        services.AddScoped<IAppUserService, AppUserService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<ISmsComposer, SmsComposer>();
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<DevelopmentIdentitySeeder>();

        return services;
    }
}
