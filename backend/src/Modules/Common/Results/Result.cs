using VeterinaryApi.Common.Errors;

namespace VeterinaryApi.Common.Results;

public class Result
{
    public bool IsSuccess { get; private set; }

    public Error Error { get; private set; }

    protected Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success => new Result(true, Errors.Error.None);

    public static Result Failure(Error error)
    {
        return new Result(false, error);
    }
}
