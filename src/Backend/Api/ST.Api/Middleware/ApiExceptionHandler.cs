using ST.Application.Exceptions;
using ST.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.ComponentModel.DataAnnotations;
using System.Net;
using DataAnnotationsValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace ST.Api.Middleware;

internal sealed class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var status = (int)HttpStatusCode.InternalServerError;
        var title = "Internal Server Error";
        var detail = "An internal server error occurred. Please contact support if the problem persists.";

        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path,
        };

        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        switch (exception)
        {
            case AuthenticationException:
                status = (int)HttpStatusCode.Unauthorized;
                title = "Authentication Failed";
                detail = "We could not sign you in. Please check your account status or contact support.";
                break;

            case UnauthorizedAccessException:
                status = (int)HttpStatusCode.Forbidden;
                title = "Forbidden";
                detail = "You are not authorized to perform this action.";
                break;

            case SecurityException:
                status = (int)HttpStatusCode.Forbidden;
                title = "Security Check Failed";
                detail = "A security check prevented this action. Please contact support if this seems wrong.";
                break;

            case CryptographicException:
                status = (int)HttpStatusCode.ServiceUnavailable;
                title = "Security Service Unavailable";
                detail = "A security service is temporarily unavailable. Please try again.";
                break;

            case ServiceUnavailableException:
                status = (int)HttpStatusCode.ServiceUnavailable;
                title = "Service Unavailable";
                detail = exception.Message;
                break;

            case ResourceNotFoundException:
            case KeyNotFoundException:
                status = (int)HttpStatusCode.NotFound;
                title = "Resource Not Found";
                detail = exception.Message;
                break;

            case DataAnnotationsValidationException validationException:
                status = (int)HttpStatusCode.UnprocessableEntity;
                title = "Validation Error";
                detail = validationException.ValidationResult?.ErrorMessage ?? exception.Message;

                var errors = new Dictionary<string, string[]>();
                foreach (var memberName in validationException.ValidationResult?.MemberNames ?? [])
                {
                    errors[memberName] = [validationException.ValidationResult?.ErrorMessage ?? "Validation error"];
                }

                problemDetails.Extensions["errors"] = errors;
                break;

            case FluentValidation.ValidationException fluentEx:
                status = (int)HttpStatusCode.BadRequest;
                title = "Validation Error";
                detail = "One or more validation errors occurred.";

                var validationErrors = fluentEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray());

                problemDetails.Extensions["errors"] = validationErrors;
                break;

            case CreatingDuplicateException:
                status = StatusCodes.Status409Conflict;
                title = "Conflict";
                detail = string.IsNullOrWhiteSpace(exception.Message)
                    ? "A resource with the same identifier already exists."
                    : exception.Message;
                break;

            case DomainException:
            case ArgumentException:
            case FormatException:
                status = (int)HttpStatusCode.BadRequest;
                title = "Bad Request";
                detail = exception.Message;
                break;

            case CustomException customException:
                status = (int)customException.StatusCode;
                title = customException.StatusCode.ToString();
                detail = customException.Message;

                if (customException.ErrorMessages is { Count: > 0 })
                {
                    problemDetails.Extensions["errors"] = customException.ErrorMessages;
                }
                break;

            case TimeoutException:
            case OperationCanceledException:
                status = (int)HttpStatusCode.GatewayTimeout;
                title = "Gateway Timeout";
                detail = "The request timed out while waiting for a downstream service. Please try again.";
                break;

            case HttpRequestException:
                status = (int)HttpStatusCode.ServiceUnavailable;
                title = "Service Unavailable";
                detail = "A downstream service is temporarily unavailable. Please try again.";
                break;

            default:
                status = (int)HttpStatusCode.InternalServerError;
                title = "Internal Server Error";
                detail = "An internal server error occurred. Please contact support if the problem persists.";
                break;

        }

        problemDetails.Status = status;
        problemDetails.Title = title;
        problemDetails.Detail = detail;
        problemDetails.Type = $"https://httpstatuses.com/{status}";
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken: cancellationToken).ConfigureAwait(false);

        return true;
    }


}
