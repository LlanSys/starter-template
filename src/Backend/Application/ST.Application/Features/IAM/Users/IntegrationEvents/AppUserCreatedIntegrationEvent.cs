using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Features.IAM.Users.IntegrationEvents;

internal sealed record AppUserCreatedIntegrationEvent(
    string UserId,
    Guid TenantId,
    string UserName,
    string FullName,
    string Email,
    DateTimeOffset OccurredAt) : IIntegrationEvent;
