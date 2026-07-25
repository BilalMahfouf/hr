using Microsoft.AspNetCore.Mvc;
using VeterinaryApi.Common.Errors;

namespace VeterinaryApi.Common.Results;

/// <summary>
/// Represents the outcome of an operation as either a success or a failure.
/// This base class is used for operations that do not return a value.
/// The <see cref="Result{T}"/> subclass is used when a value is returned on success.
/// </summary>
/// <remarks>
/// This pattern avoids using exceptions for expected business failures, keeping
/// the control flow explicit and making it clear from a method signature that
/// an operation may fail. Use <see cref="ResultExtension.Problem"/> to convert a
/// failed result into an HTTP Problem Details response.
/// </remarks>
public class Result
{
    /// <summary>Gets a value indicating whether the operation completed successfully.</summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// Gets the error associated with a failed operation.
    /// This value is <see cref="Errors.Error.None"/> when <see cref="IsSuccess"/> is <c>true</c>.
    /// </summary>
    public Error Error { get; private set; }

    /// <summary>Base constructor used by both Result and Result{T}.</summary>
    protected Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Creates a successful result with no error.</summary>
    public static Result Success => new Result(true, Errors.Error.None);

    /// <summary>
    /// Creates a failed result carrying the provided error.
    /// </summary>
    /// <param name="error">The error describing the failure reason.</param>
    /// <returns>A failed <see cref="Result"/> instance.</returns>
    public static Result Failure(Error error)
    {
        return new Result(false, error);
    }
}
