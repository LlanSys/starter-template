using System.Text.Json;
using HotChocolate;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Common.Enums;

namespace ST.Api.Common.GraphQL;

internal static class AppResponseGraphQLExtensions
{
    public static T? ToQueryResult<T>(this AppResponse<T> response)
    {
        if (response.IsSuccess)
        {
            return response.Data;
        }

        var error = response.Error ?? AppError.Unexpected();
        if (error.Type == ErrorType.NotFound)
        {
            return default;
        }

        throw ToGraphQLException(error);
    }

    public static (T? Data, IReadOnlyList<GraphQLUserError> Errors) ToMutationResult<T>(this AppResponse<T> response)
    {
        if (response.IsSuccess)
        {
            return (response.Data, []);
        }

        var error = response.Error ?? AppError.Unexpected();
        if (error is { Type: ErrorType.Validation, ValidationErrors: { Count: > 0 } fields })
        {
            return (default, fields.SelectMany(field => field.Value.Select(message =>
                new GraphQLUserError(error.Code, message, ToInputPath(field.Key)))).ToArray());
        }

        if (error.Type is ErrorType.Validation or ErrorType.BusinessRule or ErrorType.Conflict or ErrorType.NotFound)
        {
            return (default, [new GraphQLUserError(error.Code, error.Message)]);
        }

        throw ToGraphQLException(error);
    }

    private static GraphQLException ToGraphQLException(AppError error)
    {
        var message = error.Type is ErrorType.BusinessRule or ErrorType.Validation or ErrorType.Conflict
            ? error.Message
            : AppError.Unexpected().Message;

        return new GraphQLException(ErrorBuilder.New().SetMessage(message).SetCode(error.Code).Build());
    }

    private static string ToInputPath(string property)
    {
        if (property.StartsWith("Request.", StringComparison.Ordinal))
        {
            return "input." + JsonNamingPolicy.CamelCase.ConvertName(property["Request.".Length..]);
        }

        return JsonNamingPolicy.CamelCase.ConvertName(property);
    }
}
