using HotChocolate;
using HotChocolate.Types;
using MediatR;
using ST.Api.Common.Authorization;
using ST.Application.Features.ControlPlane.Stamps.Commands;
using ST.SharedKernel.Features.ControlPlane;
using ST.SharedKernel.Features.ControlPlane.Stamps.Dtos;

namespace ST.Api.Features.ControlPlane.Stamps.GraphQL;

[ExtendObjectType(OperationTypeNames.Mutation)]
internal sealed class DeploymentStampMutations
{
    [GraphQLName("createDeploymentStamp")]
    [RequireGraphQLPermission(PlatformAccess.Permission)]
    public async Task<DeploymentStampPayload> CreateDeploymentStampAsync(
        CreateDeploymentStampRequest input, [Service] ISender sender, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new CreateDeploymentStampCommand(input), cancellationToken).ConfigureAwait(false);
        return DeploymentStampPayload.From(response);
    }

    [GraphQLName("updateDeploymentStamp")]
    [RequireGraphQLPermission(PlatformAccess.Permission)]
    public async Task<DeploymentStampPayload> UpdateDeploymentStampAsync(
        Guid id, UpdateDeploymentStampRequest input, [Service] ISender sender, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new UpdateDeploymentStampCommand(id, input), cancellationToken).ConfigureAwait(false);
        return DeploymentStampPayload.From(response);
    }
}
