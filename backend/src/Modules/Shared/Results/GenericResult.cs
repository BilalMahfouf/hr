using Modules.Shared.Errors;

namespace Modules.Shared.Results;

public class Result<T> : Result
{
    private T? _value;

    private Result(bool isSuccess, T? value, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public static new Result<T> Success(T value)
    {
        return new Result<T>(true, value, Error.None);
    }

    public static new Result<T> Failure(Error error)
    {
        return new Result<T>(false, default, error);
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException(
            "Cannot access the value of a failed result.");
}
