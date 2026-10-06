using System;
using System.Collections.Generic;
using System.Text;

namespace ST.SharedKernel.Dtos.Utilities;

public static class ErrorCodes
{
    public const string Validation = "VALIDATION_ERROR";
    public const string Forbidden = "FORBIDDEN";
    public const string BusinessRule = "BUSINESS_RULE";
    public const string DependencyUnavailable = "DEPENDENCY_UNAVAILABLE";
    public const string Unexpected = "UNEXPECTED_ERROR";
    public const string NotFound = "NOT_FOUND";
}
