using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.Permissions.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Permissions.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.IAM.Permissions.Queries;



internal sealed class GetPermissionByIdQueryHandler(IIamUnitOfWork unitOfWork, ILogger<GetPermissionByIdQueryHandler> logger)
    : IRequestHandler<GetPermissionByIdQuery, AppResponse<PermissionResponse>>
{
    public async Task<AppResponse<PermissionResponse>> Handle(GetPermissionByIdQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var permission = await unitOfWork.PermissionRepository.FindByIdAsync(query.Id, cancellationToken).ConfigureAwait(false);
            return permission is null
                ? AppResponses.Failure<PermissionResponse>($"Permission {query.Id} not found.")
                : AppResponses.Success(permission.ToPermissionResponse());
        }
        catch (Exception ex)
        {
            LogDefinitions.LogPipelineException(logger, nameof(GetPermissionByIdQueryHandler), ex);
            throw;
        }
    }
}
