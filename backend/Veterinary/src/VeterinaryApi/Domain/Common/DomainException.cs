using VeterinaryApi.Common.Errors;

namespace VeterinaryApi.Domain.Common;

/// <summary>
/// Represents a domain-layer exception that carries a structured <see cref="Common.Errors.Error"/> payload.
/// Thrown when an entity's invariant is violated or an invalid state transition is attempted.
/// </summary>
/// <remarks>
/// <see cref="DomainExceptionHandler"/> intercepts this exception type in the ASP.NET Core
/// exception-handling pipeline and converts it to an HTTP 409 Conflict Problem Details response.
///
/// Prefer raising domain exceptions through entity methods (e.g., <c>Appointment.Cancel()</c>)
/// rather than constructing them directly in handlers or endpoints.
/// </remarks>
public class DomainException : Exception
{
    /// <summary>Initializes a new parameterless <see cref="DomainException"/> (EF Core compatibility).</summary>
    public DomainException()
    {
    }

    /// <summary>Gets or sets the structured error associated with this domain violation.</summary>
    public Error Error { get; set; } = null!;

    /// <summary>
    /// Initializes a new <see cref="DomainException"/> with the specified domain error.
    /// </summary>
    /// <param name="error">The domain error describing the violation. Its <c>Description</c> becomes the exception message.</param>
    public DomainException(Error error) : base(error.Description)
    {
        Error = error;
    }
}
