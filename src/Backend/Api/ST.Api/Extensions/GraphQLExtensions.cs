using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Extensions;
using HotChocolate.Types;
using ST.Api.Common.GraphQL;
using ST.Api.Features.ControlPlane.Stamps.GraphQL;

namespace ST.Api.Extensions;

internal static class GraphQLExtensions
{
    internal const string EndpointPath = "/graphql";
    internal const string ExplorerPath = "/graphql/ui";

    public static IServiceCollection AddApiGraphQL(this IServiceCollection services)
    {
        services
            .AddGraphQLServer(maxAllowedRequestSize: 64 * 1024)
            .AddAuthorization()
            .AddErrorFilter<TraceIdErrorFilter>()
            .AddQueryType(descriptor => descriptor.Name(OperationTypeNames.Query))
            .AddMutationType(descriptor => descriptor.Name(OperationTypeNames.Mutation))
            .AddTypeExtension<DeploymentStampQueries>()
            .AddTypeExtension<DeploymentStampMutations>()
            .AddType<DeploymentStampTypes.Response>()
            .AddType<DeploymentStampTypes.CreateInput>()
            .AddType<DeploymentStampTypes.UpdateInput>()
            .ModifyOptions(options => options.DefaultQueryDependencyInjectionScope = DependencyInjectionScope.Resolver)
            .ModifyRequestOptions(options =>
            {
                options.IncludeExceptionDetails = false;
                options.ExecutionTimeout = TimeSpan.FromSeconds(30);
            });

        return services;
    }

    public static void MapApiGraphQL(this WebApplication app)
    {
        app.MapGraphQLHttp(EndpointPath)
            .WithOptions(options =>
            {
                options.EnableGetRequests = false;
                options.EnableMultipartRequests = false;
            })
            .RequireAuthorization()
            .RequireRateLimiting("ApiPolicy");

        if (app.Environment.IsDevelopment())
        {
            app.MapNitroApp(ExplorerPath).WithOptions(options =>
            {
                options.GraphQLEndpoint = EndpointPath;
                options.DisableTelemetry = true;
            });
        }
    }
}
