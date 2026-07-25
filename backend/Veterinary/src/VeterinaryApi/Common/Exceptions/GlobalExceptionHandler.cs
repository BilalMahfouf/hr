using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace VeterinaryApi.Common.Exceptions;

/// <summary>
/// ASP.NET Core catch-all exception handler — the last handler in the exception pipeline.
/// Converts any unhandled exception into an RFC 7807 Problem Details response with HTTP 500 Internal Server Error,
/// while logging the full exception for diagnostics.
/// </summary>
/// <remarks>
/// This handler always returns <c>true</c>, meaning no exception will propagate to the ASP.NET Core
/// default error page or produce an empty response. The pipeline order is:
/// <list type="number">
///   <item><see cref="ValidationExceptionHandler"/> (FluentValidation → 400)</item>
///   <item><see cref="DomainExceptionHandler"/> (DomainException → 409)</item>
///   <item><see cref="GlobalExceptionHandler"/> (all others → 500)</item>
/// </list>
///
/// The error message returned to the client is deliberately generic ("Server failure") to
/// avoid leaking internal implementation details. Full exception information is available
/// only in the server-side logs.
///
/// <b>Improvement opportunity:</b> Consider using structured logging (Serilog/OpenTelemetry)
/// for better observability and trace correlation.
/// </remarks>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    /// <summary>
    /// Handles any unhandled exception by logging it and returning a generic HTTP 500 Problem Details response.
    /// This handler never returns <c>false</c>; it terminates the exception handler chain.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="exception">The unhandled exception.</param>
    /// <param name="cancellationToken">A token to observe for cooperative cancellation.</param>
    /// <returns>Always <c>true</c>, indicating the exception has been handled.</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception occurred");

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
            Title = "Server failure"
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
