using Microsoft.CodeAnalysis.Operations;
using VeterinaryApi.Common.Errors;

namespace VeterinaryApi.Domain.Users;


/// <summary>Defines domain error codes and messages for user-related operations.</summary>
public static class UserErrors
{
    /// <summary>Returned when a user with the specified <paramref name="email"/> cannot be located.</summary>
    public static Error UserNotFound(string email) =>
                Error.NotFound(
                    $"{nameof(User)}.NotFound",
                    $"User with email {email} is not found");

    /// <summary>Returned when a user with the specified <paramref name="id"/> cannot be located.</summary>
    public static Error UserNotFound(Guid id) =>
        Error.NotFound($"{nameof(User)}.NotFound",
            $"User with id {id} is not found");

    /// <summary>Returned when the provided login credentials (email/password) do not match any user.</summary>
    public static Error InvalidCredentials =>
                Error.Unauthorized(
                    $"{nameof(User)}.InvalidCredentials",
                    "The provided credentials are invalid");

    /// <summary>Returned when a refresh token is no longer valid (expired or revoked).</summary>
    public static Error ExpiredRefreshToken =>
        Error.Conflict(
            $"{nameof(User)}.ExpiredRefreshToken",
            "Refresh Token is expired, please login again");

    /// <summary>Generic not-found error when no specific identifier is available.</summary>
    public static Error NotFound =>
                Error.NotFound(
                    $"{nameof(User)}.NotFound",
                    $"User is not found");

    /// <summary>Returned when the current password supplied during a change-password request is incorrect.</summary>
    public static Error InvalidPassword =>
        Error.Conflict(
            $"{nameof(User)}.InvalidPassword",
            "The provided password is invalid");

    /// <summary>Returned when a new password does not meet the minimum length requirement (6 characters).</summary>
    public static Error InvalidPasswordLength =>
        Error.Conflict(
            $"{nameof(User)}.InvalidPasswordLength",
            "Password must be at least 6 characters long");

    /// <summary>Returned when a registration or email-change request uses an <paramref name="email"/> already registered.</summary>
    public static Error EmailAlreadyInUse(string email) =>
                Error.Conflict(
            $"{nameof(User)}.EmailAlreadyInUse",
            $"Email {email} is already in use");

    public static Error UsersNotFound =>
        Error.NotFound(
            $"{nameof(User)}.UsersNotFound",
            "No users found in the system");
}

