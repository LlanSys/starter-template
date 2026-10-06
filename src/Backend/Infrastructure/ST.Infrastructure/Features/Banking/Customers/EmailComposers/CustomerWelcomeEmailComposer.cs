using ST.SharedKernel.Features.Shared.Notifications.Dtos;
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Application.Features.Shared.Notifications.Contracts.Interfaces;
using ST.SharedKernel.Extensions;
using ST.Application.Features.Banking.Customers.IntegrationEvents;
using ST.Application.Features.HR.Employees.IntegrationEvents;
using ST.Application.Features.IAM.Users.IntegrationEvents;
using ST.Application.Utilities;
using ST.Domain.Features.Banking.Customers.Enums;
using ST.Domain.Features.Banking.Customers.Events;
using ST.Domain.Features.HR.Employees.Events;
using ST.Domain.Features.IAM.Users.Events;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Entities;
using ST.Infrastructure.Logging;
using ST.Infrastructure.Utilities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.EmailTemplates.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ST.Infrastructure.Messaging.Consumers;

namespace ST.Infrastructure.Features.Banking.Customers.EmailComposers;


internal sealed class CustomerWelcomeEmailComposer(
    ICacheService _cache,
    ISharedUnitOfWork _sharedUnitOfWork,
    ILogger<CustomerWelcomeEmailComposer> _logger) : EmailComposer<CustomerCreatedIntegrationEvent>(_cache, _sharedUnitOfWork, _logger)
{
    public override async Task<AppResponse<ComposeEmailResponse>> ComposeAsync(CustomerCreatedIntegrationEvent evt, CancellationToken ct)
    {
        var templateType = evt.Type.ToEnum<CustomerType>() switch
        {
            CustomerType.Corporate => EmailTemplateType.Corporate,
            CustomerType.Institutional => EmailTemplateType.Institutional,
            CustomerType.SmallMediumEnterprise => EmailTemplateType.SmallMediumEnterprise,
            CustomerType.Individual => EmailTemplateType.IndividualWelcome,
            CustomerType.Enterprise => EmailTemplateType.EnterpriseWelcome,
            _ => EmailTemplateType.StandardWelcome
        };

        try
        {
            var template = await ResolveTemplateAsync(templateType, ct).ConfigureAwait(false);
            if (template is null)
                return AppResponses.Failure<ComposeEmailResponse>($"Template {templateType} not found");

            var tokens = new Dictionary<string, string>
            {
                ["CustomerId"] = evt.CustomerId.ToString(),
                ["CustomerNumber"] = evt.Number,
                ["CustomerName"] = evt.Name,
                ["CustomerType"] = evt.Type,
                ["Id"] = evt.CustomerId.ToString(),
                ["Number"] = evt.Number,
                ["Name"] = evt.Name,
                ["Type"] = evt.Type,
                ["Email"] = evt.Email,
                ["Date"] = evt.OccurredAt.ToString("f", CultureInfo.InvariantCulture)
            };

            return AppResponses.Success("Email composed", ComposeFromTemplate(
                template,
                evt.Name,
                evt.Email,
                tokens));
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogEmailComposeError(_logger, templateType.ToString(), ex);
            throw;
        }
    }

}
