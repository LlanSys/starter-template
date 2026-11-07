namespace ST.Api.Common.GraphQL;

public sealed record GraphQLUserError(string Code, string Message, string? Field = null);
