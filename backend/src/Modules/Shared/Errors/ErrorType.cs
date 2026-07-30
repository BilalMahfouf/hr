namespace Modules.Shared.Errors;

public enum ErrorType
{
    None = 1,

    Validation = 400,

    NotFound = 404,

    Unauthorized = 401,

    Conflict = 409,

    Failure = 500
}
