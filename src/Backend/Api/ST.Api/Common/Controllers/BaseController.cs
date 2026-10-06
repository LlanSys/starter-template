using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Common.Enums;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using OpenTelemetry.Trace;

using System.Diagnostics;

namespace ST.Api.Common.Controllers;

public abstract class BaseController : ControllerBase
{
    protected IActionResult HandleResponse<T>(
        AppResponse<T> response,
        Func<AppResponse<T>, IActionResult>? onSuccess = null,
        Func<AppError, IActionResult>? onError = null)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.IsSuccess)
        {
            // Preserve your current success-envelope convention.
            // Change to Ok(response.Data) only if that is a deliberate API-wide decision.
            return onSuccess?.Invoke(response) ?? Ok(response);
        }

        var error = response.Error ?? AppError.Unexpected();
        return onError?.Invoke(error) ?? ToProblem(error);
    }

    private ObjectResult ToProblem(AppError error)
    {
        var (statusCode, title) = GetHttpMetadata(error.Type);

        if (error.Type == ErrorType.Validation)
        {
            var validationErrors = error.ValidationErrors is null
                ? new Dictionary<string, string[]>()
                : new Dictionary<string, string[]>(error.ValidationErrors);

            var validationProblemDetails = new ValidationProblemDetails(validationErrors)
            {
                Status = statusCode,
                Title = title,
                Detail = error.Message,
                Instance = HttpContext.Request.Path
            };

            AddDiagnostics(validationProblemDetails, error);

            return StatusCode(statusCode, validationProblemDetails);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            // Never accidentally expose internal exception details.
            Detail = error.Type == ErrorType.Unexpected
                ? "An unexpected error occurred."
                : error.Message,
            Type = ToProblemType(error.Code),
            Instance = HttpContext.Request.Path.Value
        };

        AddDiagnostics(problemDetails, error);

        return StatusCode(statusCode, problemDetails);
    }

    private static (int StatusCode, string Title) GetHttpMetadata(ErrorType type) =>
        type switch
        {
            ErrorType.Validation =>
                (StatusCodes.Status400BadRequest, "Validation failed"),

            ErrorType.NotFound =>
                (StatusCodes.Status404NotFound, "Resource not found"),

            ErrorType.Unauthorized =>
                (StatusCodes.Status401Unauthorized, "Authentication required"),

            ErrorType.Forbidden =>
                (StatusCodes.Status403Forbidden, "Access denied"),

            ErrorType.Conflict =>
                (StatusCodes.Status409Conflict, "Conflict"),

            ErrorType.BusinessRule =>
                (StatusCodes.Status422UnprocessableEntity, "Business rule violation"),

            ErrorType.DependencyUnavailable =>
                (StatusCodes.Status503ServiceUnavailable, "Dependency unavailable"),

            ErrorType.Unexpected =>
                (StatusCodes.Status500InternalServerError, "Internal server error"),

            _ =>
                (StatusCodes.Status500InternalServerError, "Internal server error")
        };

    private static string ToProblemType(string code) =>
        $"urn:bt:problem:{code.ToLowerInvariant().Replace('_', '-')}";

    private void AddDiagnostics(ProblemDetails problem, AppError error)
    {
        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    }


}
