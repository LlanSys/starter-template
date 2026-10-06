namespace ST.SharedKernel.Dtos.Common;

public static class AppResponses
{
    public static AppResponse<T> Success<T>(T? data) =>
        new(data);

    public static AppResponse<T> Success<T>(string message, T? data) =>
        new(data, message);

    public static AppResponse<T> Failure<T>(AppError error) =>
        new(error);

    public static AppResponse<T> Failure<T>(string message, T? data = default)
        => new(AppError.BusinessRule(message)) { Message = message, Data = data };

    public static AppResponse<T> NotFound<T>(string message) =>
        new(AppError.NotFound(message));

    public static AppResponse<T> ValidationFailure<T>(
        IReadOnlyDictionary<string, List<string>> validationErrors)
    {
        ArgumentNullException.ThrowIfNull(validationErrors);

        var normalizedErrors = validationErrors.ToDictionary(
            entry => entry.Key,
            entry => entry.Value
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Distinct(StringComparer.Ordinal)
                .ToArray());

        return Failure<T>(AppError.Validation(normalizedErrors));
    }

    public static AppResponse<T> ValidationFailure<T>(Dictionary<string, List<string>> validationErrors)
    {
        ArgumentNullException.ThrowIfNull(validationErrors, nameof(validationErrors));

        return ValidationFailure<T>((IReadOnlyDictionary<string, List<string>>)validationErrors);
    }
}
