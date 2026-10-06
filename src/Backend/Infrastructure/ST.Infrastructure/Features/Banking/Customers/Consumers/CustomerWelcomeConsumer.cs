using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Application.Features.Shared.Notifications.Contracts.Interfaces;
using ST.Application.Features.Banking.Customers.IntegrationEvents;
using ST.Application.Features.HR.Employees.IntegrationEvents;
using ST.Application.Features.IAM.Users.IntegrationEvents;
using ST.Domain.Shared.Contracts;
using ST.Infrastructure.Messaging.Consumers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Infrastructure.Features.Banking.Customers.Consumers;


internal sealed class CustomerWelcomeConsumer(
    IEmailComposer<CustomerCreatedIntegrationEvent> composer,
    ISharedUnitOfWork shared,
    ILogger<IntegrationEventEmailConsumer<CustomerCreatedIntegrationEvent>> logger)
    : IntegrationEventEmailConsumer<CustomerCreatedIntegrationEvent>(composer, shared, logger)
{


}
