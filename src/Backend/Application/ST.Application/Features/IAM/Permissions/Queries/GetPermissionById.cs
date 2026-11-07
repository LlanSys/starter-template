using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.Permissions.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Permissions.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.IAM.Permissions.Queries;



internal sealed class GetPermissionByIdQueryHandler(IIamUnitOfWork unitOfWork, ILogger<GetPermissionByIdQueryHandler> logger, ST.Application.Features.ControlPlane.Modules.IEffectiveModulePolicy policy)
    : IRequestHandler<GetPermissionByIdQuery, AppResponse<PermissionResponse>>
{
    public async Task<AppResponse<PermissionResponse>> Handle(GetPermissionByIdQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var access = await policy.GetAsync(cancellationToken).ConfigureAwait(false);
            var standard = access.Catalogue.FirstOrDefault(x => x.Id == query.Id && x.Kind == ST.Domain.Features.ControlPlane.Modules.Entities.ModuleCatalogueKind.Permission && access.EntryAvailable(x));
            if (standard is not null) return AppResponses.Success(ST.Application.Features.ControlPlane.Modules.ModuleCatalogueMapping.ToPermissionResponse(standard));
            var permission = await unitOfWork.PermissionRepository.FindByIdAsync(query.Id, cancellationToken).ConfigureAwait(false);
            return permission is null || !access.PermissionAvailable(permission.Key)
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
