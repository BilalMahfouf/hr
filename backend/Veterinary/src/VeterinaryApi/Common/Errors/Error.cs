namespace VeterinaryApi.Common.Errors;

/// <summary>
/// Represents a structured error with a machine-readable code, human-readable description,
/// and a category that determines the appropriate HTTP status code.
/// Used throughout the system as part of the <c>Result</c> / <c>Result&lt;T&gt;</c> pattern
/// to return errors without throwing exceptions for expected business failures.
/// </summary>
/// <remarks>
/// Create errors using the static factory methods (<see cref="Failure"/>, <see cref="NotFound"/>,
/// <see cref="Validation"/>, <see cref="Conflict"/>, <see cref="Unauthorized"/>).
/// Error instances are immutable after creation.
/// The <c>Code</c> typically follows the convention <c>"EntityName.ErrorReason"</c>
/// (e.g., <c>"Appointment.NotFound"</c>, <c>"User.InvalidCredentials"</c>).
/// </remarks>
public sealed class Error
{
    /// <summary>Gets the machine-readable error code (e.g., <c>"Appointment.NotFound"</c>).</summary>
    public string Code { get; private set; }

    /// <summary>Gets the human-readable error description for display or logging purposes.</summary>
    public string Description { get; private set; }

    /// <summary>Gets the category of this error, used to map to an HTTP status code.</summary>
    public ErrorType Type { get; private set; }

    private Error(string code, string message, ErrorType type)
    {
        Type = type;
        Code = code;
        Description = message;
    }

    /// <summary>
    /// Gets the singleton "no error" value, used internally to represent a successful result.
    /// </summary>
    public static Error None =>
        new("Error.None", "No error.", ErrorType.None);

    /// <summary>
    /// Creates a generic failure error (HTTP 500).
    /// Use for unexpected infrastructure or application errors.
    /// </summary>
    /// <param name="code">The machine-readable error code.</param>
    /// <param name="description">A description of the failure.</param>
    public static Error Failure(string code, string description) =>
        new(code, description, ErrorType.Failure);

    /// <summary>
    /// Creates a not-found error (HTTP 404).
    /// Use when a queried resource cannot be located.
    /// </summary>
    /// <param name="code">The machine-readable error code.</param>
    /// <param name="description">A description explaining which resource was not found.</param>
    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    /// <summary>
    /// Creates a validation error (HTTP 400).
    /// Use when client-supplied input violates a business or format rule.
    /// </summary>
    /// <param name="code">The machine-readable error code.</param>
    /// <param name="description">A description of the validation failure.</param>
    public static Error Validation(string code, string description) =>
        new(code, description, ErrorType.Validation);

    /// <summary>
    /// Creates a conflict error (HTTP 409).
    /// Use when a domain rule is violated or the entity is in an incompatible state.
    /// </summary>
    /// <param name="code">The machine-readable error code.</param>
    /// <param name="description">A description of the conflict.</param>
    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    /// <summary>
    /// Creates an unauthorized error (HTTP 401).
    /// Use when the caller lacks the required authentication or permission.
    /// </summary>
    /// <param name="code">The machine-readable error code.</param>
    /// <param name="description">A description of the authorization failure.</param>
    public static Error Unauthorized(string code, string description) =>
        new(code, description, ErrorType.Unauthorized);
}
