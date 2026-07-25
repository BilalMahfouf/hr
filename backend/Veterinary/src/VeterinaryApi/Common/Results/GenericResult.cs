using VeterinaryApi.Common.Errors;

namespace VeterinaryApi.Common.Results;

/// <summary>
/// Represents the outcome of an operation that returns a typed value on success.
/// Inherits from <see cref="Result"/> and adds a <see cref="Value"/> property
/// accessible only when the operation succeeded.
/// </summary>
/// <typeparam name="T">The type of the value returned on a successful operation.</typeparam>
/// <remarks>
/// Use <c>Result&lt;T&gt;.Success(value)</c> to create a successful result
/// and <c>Result&lt;T&gt;.Failure(error)</c> to create a failed result.
/// Attempting to access <see cref="Value"/> on a failed result throws
/// <see cref="InvalidOperationException"/> — always check <see cref="Result.IsSuccess"/> first,
/// or use pattern matching on the result.
/// </remarks>
public class Result<T> : Result
{
    private T? _value;

    private Result(bool isSuccess, T? value, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Creates a successful result carrying the specified value.
    /// The <see cref="Result.Error"/> will be set to <see cref="Error.None"/>.
    /// </summary>
    /// <param name="value">The value to return from the successful operation.</param>
    /// <returns>A successful <see cref="Result{T}"/> containing <paramref name="value"/>.</returns>
    public static new Result<T> Success(T value)
    {
        return new Result<T>(true, value, Error.None);
    }

    /// <summary>
    /// Creates a failed result carrying the specified error.
    /// The <see cref="Value"/> property will throw if accessed.
    /// </summary>
    /// <param name="error">The <see cref="Error"/> describing why the operation failed.</param>
    /// <returns>A failed <see cref="Result{T}"/> with no value.</returns>
    public static new Result<T> Failure(Error error)
    {
        return new Result<T>(false, default, error);
    }

    /// <summary>
    /// Gets the value returned by the successful operation.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when accessed on a failed result (i.e., <see cref="Result.IsSuccess"/> is <c>false</c>).
    /// Always check <see cref="Result.IsSuccess"/> before accessing this property.
    /// </exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException(
            "Cannot access the value of a failed result.");
}
