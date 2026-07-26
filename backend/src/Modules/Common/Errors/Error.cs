namespace VeterinaryApi.Common.Errors;

public sealed class Error
{
    public string Code { get; private set; }

    public string Description { get; private set; }

    public ErrorType Type { get; private set; }

    private Error(string code, string message, ErrorType type)
    {
        Type = type;
        Code = code;
        Description = message;
    }

    public static Error None =>
        new("Error.None", "No error.", ErrorType.None);

    public static Error Failure(string code, string description) =>
        new(code, description, ErrorType.Failure);

    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    public static Error Validation(string code, string description) =>
        new(code, description, ErrorType.Validation);

    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    public static Error Unauthorized(string code, string description) =>
        new(code, description, ErrorType.Unauthorized);
}
