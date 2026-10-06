using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Features.IAM.Users.Commands;


public sealed record CreateAppUserCommand(CreateAppUserRequest Request, string UserId)
    : IRequest<AppResponse<AppUserResponse>>, ICacheInvalidatorRequest
{
    public IReadOnlyList<string> DirectInvalidationKeys => [];

    public IReadOnlyList<string> GroupVersionKeysToInvalidate => [CacheKeys.GroupVersion("iam-admin")];
}
