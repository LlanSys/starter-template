using ST.SharedKernel.Dtos.Common;
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.Banking.Customers.Entities;
using ST.SharedKernel.Features.Banking.Customers.Dtos;

using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Features.Banking.Customers.CommandHandlers;

/// <summary>
/// Same invalidation pattern as Update: remove the entity entry + bump the list version.
/// </summary>

public record DeleteCustomerCommand(Guid CustomerId, string UserId) : IRequest<AppResponse<bool>>, ICacheInvalidatorRequest

{
    public IReadOnlyList<string> DirectInvalidationKeys => [CacheKeys.Entity("customers", CustomerId.ToString())];
    public IReadOnlyList<string> GroupVersionKeysToInvalidate =>
    [
        CacheKeys.GroupVersion("customers"),
        CacheKeys.GroupVersion("dashboard")
    ];

}
