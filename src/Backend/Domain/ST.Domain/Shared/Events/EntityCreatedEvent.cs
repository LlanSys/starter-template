using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Domain.Shared.Events;

public sealed record EntityCreatedEvent(DateTimeOffset OccurredAt) : IDomainEvent;
