using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Domain.Shared.Contracts.Common;

public interface IIntegrationEvent : INotification
{
    DateTimeOffset OccurredAt { get; }
}
