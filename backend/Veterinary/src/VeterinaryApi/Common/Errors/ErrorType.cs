namespace VeterinaryApi.Common.Errors;

/// <summary>
/// Enumerates the possible categories of errors that can occur in the system.
/// Each value maps to a corresponding HTTP status code and is used by
/// <c>ResultExtension.Problem()</c> to produce RFC 7807-compliant Problem Details responses.
/// </summary>
public enum ErrorType
{
    /// <summary>Indicates no error occurred. Used by <c>Error.None</c> in successful results.</summary>
    None = 1,

    /// <summary>
    /// Indicates a client-provided input validation error (HTTP 400 Bad Request).
    /// Typically produced by FluentValidation failures surfaced via the Result pattern.
    /// </summary>
    Validation = 400,

    /// <summary>
    /// Indicates that a requested resource could not be found (HTTP 404 Not Found).
    /// Used when querying for an entity by ID that does not exist.
    /// </summary>
    NotFound = 404,

    /// <summary>
    /// Indicates that the request requires authentication or the requester lacks permissions
    /// (HTTP 401 Unauthorized).
    /// </summary>
    Unauthorized = 401,

    /// <summary>
    /// Indicates a business rule conflict or state violation (HTTP 409 Conflict).
    /// Typically thrown as a <c>DomainException</c> from within entity business methods.
    /// </summary>
    Conflict = 409,

    /// <summary>
    /// Indicates an unexpected infrastructure or application failure (HTTP 500 Internal Server Error).
    /// </summary>
    Failure = 500
}
