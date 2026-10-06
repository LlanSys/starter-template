using System;
using System.Text.Json.Serialization;

namespace ST.SharedKernel.Dtos.Common;



public record AppResponse<T> : IAppResponse
{
    public bool IsSuccess { get; init; }
    public string? Message { get; init; }
    public string? SessionId { get; init; }
    public T? Data { get; init; }

    // Failures are converted to ProblemDetails by BaseController.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AppError? Error { get; init; }

    public AppResponse() { }

    public AppResponse(T? data)
    {
        IsSuccess = true;
        Message = "Operation Successful";
        Data = data;
    }

    [JsonConstructor]
    internal AppResponse(bool isSuccess, string? message, T? data, AppError? error)
    {
        IsSuccess = isSuccess;
        Message = message ?? (isSuccess ? "Operation Successful" : null);
        Data = data;
        Error = error;
    }



    internal AppResponse(AppError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        IsSuccess = false;
        Error = error;
    }

    public AppResponse(T? data, string message)
    {
        IsSuccess = true;
        Data = data;
        Message = message;
    }
}
