using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.IAM.Users.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Domain.Features.IAM.Users.Events;

public sealed record RequestPasswordResetEvent(
    Guid UserId,
    string FirstName,
    string PhoneNumber,
    string ValidationCode,
    string Email,
    string ResetToken,
    string ResetUrl,
    MfaChannel PreferredChannel

) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
