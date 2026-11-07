using HotChocolate;
using ST.Api.Common.GraphQL;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Stamps.Dtos;

namespace ST.Api.Features.ControlPlane.Stamps.GraphQL;

public sealed record DeploymentStampPayload(DeploymentStampResponse? Stamp, IReadOnlyList<GraphQLUserError> Errors)
{
    [GraphQLIgnore]
    internal static DeploymentStampPayload From(AppResponse<DeploymentStampResponse> response)
    {
        var (stamp, errors) = response.ToMutationResult();
        return new(stamp, errors);
    }
}
