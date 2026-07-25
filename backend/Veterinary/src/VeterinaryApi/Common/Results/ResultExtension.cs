using VeterinaryApi.Common.Errors;

namespace VeterinaryApi.Common.Results;

/// <summary>
/// Extension methods that bridge the domain <see cref="Result"/> pattern with
/// ASP.NET Core Minimal API response types (<see cref="IResult"/>).
/// </summary>
public static class ResultExtension
{
    /// <summary>
    /// Converts a failed <see cref="Result"/> into an RFC 7807 Problem Details <see cref="IResult"/>
    /// with an appropriate HTTP status code derived from the result's <see cref="Error.Type"/>.
    /// </summary>
    /// <param name="result">The failed result to convert. Must not be a success result.</param>
    /// <returns>
    /// An <see cref="IResult"/> containing a Problem Details response body and the matching HTTP status code.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="result"/> represents a success (i.e., <see cref="Result.IsSuccess"/> is <c>true</c>).
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the result's <see cref="Result.Error"/> property is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// Route handlers should call this only after confirming the result is a failure:
    /// <code>
    /// if (result.IsFailure) return result.Problem();
    /// </code>
    /// </remarks>
    public static IResult Problem(this Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException();
        }
        if (result.Error is null)
        {
            throw new ArgumentNullException();
        }

        return Microsoft.AspNetCore.Http.Results.Problem(
            title: GetTitle(result.Error),
            type: GetType(result.Error.Type),
            statusCode: GetStatusCode(result.Error.Type),
            extensions: GetErrors(result));

        /// <summary>Returns the problem title which maps to the error's code string.</summary>
        static string GetTitle(Error error) =>
            error.Type switch
            {
                ErrorType.Validation => error.Code,
                ErrorType.Unauthorized => error.Code,
                ErrorType.NotFound => error.Code,
                ErrorType.Conflict => error.Code,
                _ => "Server failure"
            };

        /// <summary>Returns a human-readable detail message for the error type.</summary>
        static string GetDetail(Error error) =>
            error.Type switch
            {
                ErrorType.Validation => error.Description,
                ErrorType.Unauthorized => error.Description,
                ErrorType.NotFound => error.Description,
                ErrorType.Conflict => error.Description,
                _ => "An unexpected error occurred"
            };

        /// <summary>Returns the RFC 7231 URI reference corresponding to the HTTP status class.</summary>
        static string GetType(ErrorType errorType) =>
            errorType switch
            {
                ErrorType.Validation => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                ErrorType.Unauthorized => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                ErrorType.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                ErrorType.Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            };

        /// <summary>Maps an <see cref="ErrorType"/> to its corresponding HTTP status code integer.</summary>
        static int GetStatusCode(ErrorType errorType) =>
            errorType switch
            {
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status500InternalServerError
            };

        /// <summary>
        /// Builds the <c>extensions</c> dictionary that includes the error code and description
        /// in the Problem Details response body under the key <c>"errors"</c>.
        /// </summary>
        static Dictionary<string, object?>? GetErrors(Result result)
        {
            return new Dictionary<string, object?>
            {
                { "errors", new[] {
                    result.Error.Code,
                    result.Error.Description
                } }
            };
        }
    }
}
