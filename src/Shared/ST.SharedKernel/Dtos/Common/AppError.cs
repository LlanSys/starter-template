
using ST.SharedKernel.Dtos.Utilities;
using ST.SharedKernel.Features.Shared.Common.Enums;

using System;
using System.Collections.Generic;
using System.Text;

namespace ST.SharedKernel.Dtos.Common;

public sealed record AppError(ErrorType Type, string Code, string Message, IReadOnlyDictionary<string, string[]>? ValidationErrors = null)
{
    public static AppError Validation(IReadOnlyDictionary<string, string[]> validationErrors)
    {
        ArgumentNullException.ThrowIfNull(validationErrors);

        return new AppError(
            ErrorType.Validation,
            ErrorCodes.Validation,
            "One or more validation failures have occurred.",
            validationErrors);
    }

    public static AppError Unexpected() =>
        new(
            ErrorType.Unexpected,
            ErrorCodes.Unexpected,
            "An unexpected error occurred.");

    public static AppError NotFound(string message) =>
        new(
            ErrorType.NotFound,
            ErrorCodes.NotFound,
            message);

    public static AppError Forbidden(string message) =>
        new(
            ErrorType.Forbidden,
            ErrorCodes.Forbidden,
            message);

    public static AppError BusinessRule(string message) =>
        new(
            ErrorType.BusinessRule,
            ErrorCodes.BusinessRule,
            message);

    public static AppError DependencyUnavailable(string message) =>
        new(
            ErrorType.DependencyUnavailable,
            ErrorCodes.DependencyUnavailable,
            message);
}
