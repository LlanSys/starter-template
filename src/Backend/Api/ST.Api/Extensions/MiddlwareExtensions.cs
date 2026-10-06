using ST.Api.Middleware;

namespace ST.Api.Extensions;

internal static class MiddlwareExtensions
{
    public static IApplicationBuilder UsePostAuthMiddleware(this IApplicationBuilder builder)
    {
        return builder
            .UseMiddleware<SessionValidationMiddleware>()
            .UseMiddleware<MfaEnrollmentMiddleware>();
    }
}
