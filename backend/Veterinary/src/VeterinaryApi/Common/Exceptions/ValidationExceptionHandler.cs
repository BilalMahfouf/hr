using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace VeterinaryApi.Common.Exceptions;

/// <summary>
/// ASP.NET Core exception handler that intercepts <see cref="FluentValidation.ValidationException"/>
/// instances and converts them into RFC 7807 Problem Details responses with HTTP 400 Bad Request.
/// </summary>
/// <remarks>
/// This handler is the first in the exception handler pipeline (registered before
/// <see cref="DomainExceptionHandler"/> and <see cref="GlobalExceptionHandler"/>).
/// It returns <c>false</c> for any exception that is not a <see cref="ValidationException"/>,
/// allowing subsequent handlers to process it.
///
/// The response body includes an <c>"errors"</c> extension dictionary where each key is the
/// lowercased property name that failed validation and the value is an array of error messages,
/// following the standard ASP.NET Core validation problem format.
///
/// <b>Example response body:</b>
/// <code>
/// {
///   "status": 400,
///   "detail": "One or more validation errors occured",
///   "errors": {
///     "email": ["'Email' must not be empty.", "'Email' is not a valid email address."]
///   }
/// }
/// </code>
/// </remarks>
public sealed class ValidationExceptionHandler(
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    /// <summary>
    /// Attempts to handle a <see cref="ValidationException"/>.
    /// Sets the HTTP status to 400 and writes a Problem Details response including per-field errors.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="cancellationToken">A token to observe for cooperative cancellation.</param>
    /// <returns>
    /// <c>true</c> if the exception was a <see cref="ValidationException"/> and was handled;
    /// <c>false</c> otherwise, allowing the next registered handler to process the exception.
    /// </returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Detail = "One or more validation errors occured",
                Status = StatusCodes.Status400BadRequest
            }
        };

        var errors = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key.ToLowerInvariant(),
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        context.ProblemDetails.Extensions.Add("errors", errors);

        return await problemDetailsService.TryWriteAsync(context);
    }
}
