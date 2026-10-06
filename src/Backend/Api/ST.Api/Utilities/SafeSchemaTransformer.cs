using ST.Api.Logging;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ST.Api.Utilities;

internal sealed class SafeSchemaTransformer(ILogger<SafeSchemaTransformer> logger) : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (schema is null) return Task.CompletedTask;

        // Clean up malformed properties
        // Using property pattern matching to ensure it exists and has items to process
        if (schema.Properties is { Count: > 0 })
        {
            // We must materialize the keys to remove because we cannot mutate a dictionary while iterating over it.
            var invalidKeys = schema.Properties
                .Where(kvp => string.IsNullOrEmpty(kvp.Key) || kvp.Value is null)
                .Select(static kvp => kvp.Key)
                .ToList();

            foreach (var key in invalidKeys)
            {
                schema.Properties.Remove(key);

                MiddlewareLogDefinitions.LogRemovedInvalidSchemaProperty(logger, key, context.JsonTypeInfo?.Type.Name ?? "Unknown");
            }
        }

        // Clean up broken AdditionalProperties
        // Pattern matching 'is { } addProps' ensures it's not null and assigns it to a local variable
        if (schema.AdditionalPropertiesAllowed && schema.AdditionalProperties is { } addProps)
        {
            // Type is a nullable [Flags] enum (JsonSchemaType?).
            // There is no "None" flag. An unset type is null; a glitch type is often Null
            if (addProps.Type is null or JsonSchemaType.Null)
            {
                schema.AdditionalProperties = null;
            }
        }

        return Task.CompletedTask;
    }
}