using ST.Application.Features.Banking.Customers.IntegrationEvents;
using ST.Application.Features.Shared.Notifications.Contracts.Interfaces;
using ST.Infrastructure.Features.Banking.Customers.EmailComposers;
using ST.Persistence.Features.Banking.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ST.Infrastructure.Features.Banking.Extensions;

public static class BankingModuleDI
{
    public static IServiceCollection AddBankingModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddBankingPersistence(configuration);
        services.AddScoped<IEmailComposer<CustomerCreatedIntegrationEvent>, CustomerWelcomeEmailComposer>();
        return services;
    }
}
