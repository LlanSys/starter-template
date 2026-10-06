using ST.SharedKernel.Features.Shared.Notifications.Dtos;
using ST.Application.Features.IAM.Users.IntegrationEvents;
using ST.Domain.Features.IAM.Users.Events;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Shared.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.EmailTemplates.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Features.Shared.Notifications.Contracts.Interfaces;

public interface IEmailComposer<TEvent> where TEvent : IIntegrationEvent
{
    Task<AppResponse<ComposeEmailResponse>> ComposeAsync(TEvent evt, CancellationToken ct);
    Task<EmailTemplate?> ResolveTemplateAsync(EmailTemplateType emailTemplateType, CancellationToken cancellationToken);

}
