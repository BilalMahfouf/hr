using Microsoft.EntityFrameworkCore;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Common.CQRS;
using VeterinaryApi.Common.Endpoints;
using VeterinaryApi.Common.Results;
using VeterinaryApi.Domain.Users;

namespace VeterinaryApi.Features.Users;

/// <summary>
/// Vertical slice for completing a password reset using a one-time token.
/// Verifies the reset token against the <see cref="UserSession"/> table and updates the password hash.
/// </summary>
public static class ResetPassword
{
    /// <summary>
    /// Command carrying all data required to perform the password reset.
    /// Implements the non-generic <see cref="ICommand"/> (no return value).
    /// </summary>
    /// <param name="Password">The new plain-text password.</param>
    /// <param name="ConfirmPassword">Must match <paramref name="Password"/>; caller or a validator should enforce equality.</param>
    /// <param name="Token">The reset token received from the password-reset email link.</param>
    /// <param name="Email">The email address identifying the account to reset.</param>
    public sealed record ResetPasswordCommand(
        string Password,
        string ConfirmPassword,
        string Token,
        string Email) : ICommand;

    /// <summary>
    /// Handles the <see cref="ResetPasswordCommand"/> by validating the token and updating the password.
    /// </summary>
    public sealed class ResetPasswordCommandHandler
        : ICommandHandler<ResetPasswordCommand>
    {
        private readonly IApplicationDbContext _db;
        private readonly IPasswordHasher _passwordHasher;

        /// <summary>Initializes the handler with database and password hashing services.</summary>
        public ResetPasswordCommandHandler(
            IApplicationDbContext db,
            IPasswordHasher passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
        }

        /// <summary>
        /// Executes the reset-password flow:
        /// <list type="number">
        ///   <item>Loads the user by email.</item>
        ///   <item>Validates the token against <see cref="UserSession"/> rows of type <see cref="UserSessionTokenType.ResetPassword"/> that have not expired.</item>
        ///   <item>Hashes the new password and calls <c>User.UpdatePassword()</c>.</item>
        ///   <item>Persists the change. Note: the consumed reset session is not deleted — expired sessions should be cleaned up by a background job.</item>
        /// </list>
        /// </summary>
        /// <param name="command">The reset-password command.</param>
        /// <param name="cancellationToken">Token for cooperative cancellation.</param>
        /// <returns>
        /// A successful result, or a failure with <c>UserErrors.UserNotFound</c> or
        /// <c>UserErrors.InvalidCredentials</c> if the token is invalid or expired.
        /// </returns>
        public async Task<Result> Handle(
            ResetPasswordCommand command,
            CancellationToken cancellationToken = default)
        {
            var user = await _db.Users.FirstOrDefaultAsync(
                u => u.Email == command.Email,
                cancellationToken);
            if (user is null)
            {
                return Result.Failure(UserErrors.UserNotFound(command.Email));
            }
            var isValidToken = await _db.UserSessions
                .AnyAsync(u => u.Token == command.Token &&
            u.ExpiresAt > DateTime.UtcNow
            && u.TokenType == UserSessionTokenType.ResetPassword);
            if (!isValidToken)
            {
                return Result.Failure(UserErrors.InvalidCredentials);
            }
            var newPasswordHash = _passwordHasher.Hash(command.Password);
            user.UpdatePassword(command.Password, newPasswordHash);
            _db.Users.Update(user);

            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
    }

    /// <summary>
    /// Carter endpoint that maps <c>PUT /auth/reset-passowrd</c>.
    /// Note: the route contains a typo (<c>passowrd</c> instead of <c>password</c>).
    /// Returns <c>200 OK</c> on success or Problem Details on failure.
    /// </summary>
    public sealed class Endpoint : IEndpoint
    {
        /// <summary>Registers the reset-password route.</summary>
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPut("/auth/reset-passowrd", async (
                ResetPasswordCommand command,
                ICommandHandler<ResetPasswordCommand> handler,
                CancellationToken cancellationToken = default) =>
            {
                var result = await handler.Handle(command, cancellationToken);
                return result.IsSuccess ? Results.Ok()
                                 : result.Problem();

            })
            .WithTags("Authentication")
            .WithSummary("Reset password")
            .WithDescription("Resets the user's password using a valid reset token. Requires matching password and confirm password fields.");
        }
    }
}
