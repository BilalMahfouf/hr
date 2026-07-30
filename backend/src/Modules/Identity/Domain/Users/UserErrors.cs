using Shared.Errors;

namespace Identity.Domain.Users;

public static class UserErrors
{
    public static Error UserNotFound(string email) =>
                Error.NotFound(
                    $"{nameof(User)}.NotFound",
                    $"User with email {email} is not found");

    public static Error UserNotFound(Guid id) =>
        Error.NotFound($"{nameof(User)}.NotFound",
            $"User with id {id} is not found");

    public static Error InvalidCredentials =>
                Error.Unauthorized(
                    $"{nameof(User)}.InvalidCredentials",
                    "The provided credentials are invalid");

    public static Error ExpiredRefreshToken =>
        Error.Conflict(
            $"{nameof(User)}.ExpiredRefreshToken",
            "Refresh Token is expired, please login again");

    public static Error NotFound =>
                Error.NotFound(
                    $"{nameof(User)}.NotFound",
                    $"User is not found");

    public static Error InvalidPassword =>
        Error.Conflict(
            $"{nameof(User)}.InvalidPassword",
            "The provided password is invalid");

    public static Error InvalidPasswordLength =>
        Error.Conflict(
            $"{nameof(User)}.InvalidPasswordLength",
            "Password must be at least 6 characters long");

    public static Error EmailAlreadyInUse(string email) =>
                Error.Conflict(
            $"{nameof(User)}.EmailAlreadyInUse",
            $"Email {email} is already in use");

    public static Error UsersNotFound =>
        Error.NotFound(
            $"{nameof(User)}.UsersNotFound",
            "No users found in the system");
}
