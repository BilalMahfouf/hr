using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VeterinaryApi.Common.Errors;
using VeterinaryApi.Domain.Common;

namespace VeterinaryApi.Common.Exceptions;

/// <summary>
/// ASP.NET Core exception handler that intercepts <see cref="DomainException"/> instances
/// and converts them into RFC 7807 Problem Details responses with HTTP 409 Conflict.
/// </summary>
/// <remarks>
/// Domain exceptions represent invariant violations within the domain model — for example,
/// attempting to cancel an already-completed appointment. This handler is the second in the
/// exception handler pipeline (registered after <see cref="ValidationExceptionHandler"/> and
/// before <see cref="GlobalExceptionHandler"/>).
///
/// It returns <c>false</c> for any exception that is not a <see cref="DomainException"/>,
/// allowing subsequent handlers to process it.
///
/// The response includes the error's code as the <c>title</c>, an RFC 7231 type URI,
/// and the full <see cref="Error"/> object in the <c>"errors"</c> extension field.
/// </remarks>
public class DomainExceptionHandler(
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    /// <summary>
    /// Attempts to handle a <see cref="DomainException"/>.
    /// Sets the HTTP status to 409 Conflict and writes a Problem Details response with the domain error details.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="cancellationToken">A token to observe for cooperative cancellation.</param>
    /// <returns>
    /// <c>true</c> if the exception was a <see cref="DomainException"/> and was handled;
    /// <c>false</c> otherwise, allowing the next registered handler to process the exception.
    /// </returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DomainException domainException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Title = domainException.Error.Code,
                Detail = "One or more conflicts  occured",
                Status = StatusCodes.Status409Conflict,
                Type = GetType(domainException.Error.Type)
            }
        };

        context.ProblemDetails.Extensions.Add("errors", domainException.Error);

        return await problemDetailsService.TryWriteAsync(context);
    }

    /// <summary>
    /// Maps an <see cref="ErrorType"/> to its corresponding RFC 7231 URI reference string
    /// for inclusion in the Problem Details <c>type</c> field.
    /// </summary>
    /// <param name="errorType">The domain error type to map.</param>
    /// <returns>An RFC 7231 URI string for the matching HTTP error class.</returns>
    static string GetType(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Validation => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            ErrorType.Unauthorized => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            ErrorType.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            ErrorType.Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };
}
