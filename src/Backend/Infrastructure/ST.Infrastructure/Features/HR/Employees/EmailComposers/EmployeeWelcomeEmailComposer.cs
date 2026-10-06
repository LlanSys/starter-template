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
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Entities;
using ST.Infrastructure.Logging;
using ST.Infrastructure.Messaging.Consumers;
using ST.Infrastructure.Utilities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.EmailTemplates.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ST.Infrastructure.Features.HR.Employees.EmailComposers;


internal sealed class EmployeeWelcomeEmailComposer(
    ICacheService _cache,
    ISharedUnitOfWork _sharedUnitOfWork,
    ILogger<EmployeeWelcomeEmailComposer> _logger) : EmailComposer<EmployeeCreatedIntegrationEvent>(_cache, _sharedUnitOfWork, _logger)
{
    public override async Task<AppResponse<ComposeEmailResponse>> ComposeAsync(EmployeeCreatedIntegrationEvent evt, CancellationToken ct)
    {
        var templateType = EmailTemplateType.StandardWelcome;

        try
        {
            var template = await ResolveTemplateAsync(templateType, ct).ConfigureAwait(false);

            if (template == null)
                return AppResponses.Failure<ComposeEmailResponse>($"Template {templateType} not found");

            var tokens = new Dictionary<string, string>
            {
                ["EmployeeId"] = evt.EmployeeId.ToString(),
                ["EmployeeNumber"] = evt.Number,
                ["EmployeeName"] = evt.Name,
                ["Id"] = evt.EmployeeId.ToString(),
                ["Number"] = evt.Number,
                ["Name"] = evt.Name,
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
