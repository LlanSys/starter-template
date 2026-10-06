using ST.Application.Features.HR.Employees.IntegrationEvents;
using ST.Application.Features.Shared.Notifications.Contracts.Interfaces;
using ST.Infrastructure.Features.HR.Employees.EmailComposers;
using ST.Persistence.Features.HR.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ST.Infrastructure.Features.HR.Extensions;

public static class HrModuleDI
{
    public static IServiceCollection AddHrModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHrPersistence(configuration);
        services.AddScoped<IEmailComposer<EmployeeCreatedIntegrationEvent>, EmployeeWelcomeEmailComposer>();
        return services;
    }
}
