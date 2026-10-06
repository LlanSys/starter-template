using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ST.Domain.Shared.Contracts.Common;

public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    [SuppressMessage("Design", "CA1030:Use events where appropriate",
        Justification = "This method collects domain events following DDD pattern, not a C# event")]
    void RaiseDomainEvent(IDomainEvent domainEvent);
    void ClearDomainEvents();
}
