using HotChocolate.Types;
using ST.SharedKernel.Features.ControlPlane.Stamps.Dtos;

namespace ST.Api.Features.ControlPlane.Stamps.GraphQL;

internal static class DeploymentStampTypes
{
    public sealed class Response : ObjectType<DeploymentStampResponse>
    {
        protected override void Configure(IObjectTypeDescriptor<DeploymentStampResponse> descriptor)
        {
            descriptor.Name("DeploymentStamp").BindFieldsExplicitly();
            descriptor.Field(stamp => stamp.Id);
            descriptor.Field(stamp => stamp.Name);
            descriptor.Field(stamp => stamp.TargetResourceGroup);
            descriptor.Field(stamp => stamp.IsolationTier);
            descriptor.Field(stamp => stamp.KeyVaultUri);
            descriptor.Field(stamp => stamp.DatabaseProvider);
            descriptor.Field(stamp => stamp.CreatedAt);
            descriptor.Field(stamp => stamp.UpdatedAt);
        }
    }

    public sealed class CreateInput : InputObjectType<CreateDeploymentStampRequest>
    {
        protected override void Configure(IInputObjectTypeDescriptor<CreateDeploymentStampRequest> descriptor)
        {
            descriptor.Name("CreateDeploymentStampInput");
        }
    }

    public sealed class UpdateInput : InputObjectType<UpdateDeploymentStampRequest>
    {
        protected override void Configure(IInputObjectTypeDescriptor<UpdateDeploymentStampRequest> descriptor)
        {
            descriptor.Name("UpdateDeploymentStampInput");
        }
    }
}
