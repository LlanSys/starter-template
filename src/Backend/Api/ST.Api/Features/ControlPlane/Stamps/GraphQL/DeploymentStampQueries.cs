using HotChocolate;
using HotChocolate.Types;
using MediatR;
using ST.Api.Common.Authorization;
using ST.Api.Common.GraphQL;
using ST.Application.Features.ControlPlane.Stamps.Queries;
using ST.SharedKernel.Features.ControlPlane;
using ST.SharedKernel.Features.ControlPlane.Stamps.Dtos;

namespace ST.Api.Features.ControlPlane.Stamps.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
internal sealed class DeploymentStampQueries
{
    [GraphQLName("deploymentStamps")]
    [RequireGraphQLPermission(PlatformAccess.Permission)]
    public async Task<IReadOnlyList<DeploymentStampResponse>> GetDeploymentStampsAsync(
        [Service] ISender sender, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new GetAllDeploymentStampsQuery(), cancellationToken).ConfigureAwait(false);
        return response.ToQueryResult() ?? [];
    }

    [GraphQLName("deploymentStamp")]
    [RequireGraphQLPermission(PlatformAccess.Permission)]
    public async Task<DeploymentStampResponse?> GetDeploymentStampAsync(
        Guid id, [Service] ISender sender, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new GetDeploymentStampByIdQuery(id), cancellationToken).ConfigureAwait(false);
        return response.ToQueryResult();
    }
}
