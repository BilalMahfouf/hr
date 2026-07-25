using Microsoft.EntityFrameworkCore;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Common.CQRS;
using VeterinaryApi.Common.Endpoints;
using VeterinaryApi.Common.Errors;
using VeterinaryApi.Common.Results;
using VeterinaryApi.Domain.Users;

namespace VeterinaryApi.Features.Users;

/// <summary>
/// Vertical slice for changing the authenticated user's password.
/// Requires the current password for verification before accepting the new password.
/// </summary>
public static class ChangePassword
{
    /// <summary>
    /// Command carrying the current and new passwords.
    /// Implements the non-generic <see cref="ICommand"/> (no return value).
    /// </summary>
    /// <param name="CurrentPassword">The user's existing plain-text password used to verify identity.</param>
    /// <param name="NewPassword">The new plain-text password to hash and store.</param>
    /// <param name="ConfirmNewPassword">Must match <paramref name="NewPassword"/>; validation is expected in the caller or a validator.</param>
    public sealed record ChangePasswordCommand(
        string CurrentPassword,
        string NewPassword,
        string ConfirmNewPassword) : ICommand;

    /// <summary>
    /// Handles the <see cref="ChangePasswordCommand"/> by verifying the current password
    /// and then updating to the new Argon2 hash.
    /// </summary>
    public sealed class ChangePasswordCommandHandler
        : ICommandHandler<ChangePasswordCommand>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentTenant _currentTenant;
        private readonly IPasswordHasher _passwordHasher;

        /// <summary>Initializes the handler with database, tenant context, and password hashing services.</summary>
        public ChangePasswordCommandHandler(
            IApplicationDbContext db,
            ICurrentTenant currentTenant,
            IPasswordHasher passwordHasher)
        {
            _db = db;
            _currentTenant = currentTenant;
            _passwordHasher = passwordHasher;
        }

        /// <summary>
        /// Executes the password change flow:
        /// <list type="number">
        ///   <item>Resolves the current user ID from <see cref="ICurrentTenant"/>.</item>
        ///   <item>Loads the user entity from the database.</item>
        ///   <item>Verifies the <paramref name="command"/>.CurrentPassword against the stored Argon2 hash.</item>
        ///   <item>Hashes the new password and calls <c>User.UpdatePassword()</c>.</item>
        ///   <item>Persists the change.</item>
        /// </list>
        /// </summary>
        /// <param name="command">The change-password command.</param>
        /// <param name="cancellationToken">Token for cooperative cancellation.</param>
        /// <returns>A successful result, or a failure with <c>UserErrors.NotFound</c> or <c>UserErrors.InvalidPassword</c>.</returns>
        public async Task<Result> Handle(
            ChangePasswordCommand command,
            CancellationToken cancellationToken = default)
        {
            var userId = _currentTenant.UserId;
            var user = await _db.Users
                .FirstOrDefaultAsync(e => e.Id == userId, cancellationToken);
            if (user is null)
            {
                return Result.Failure(UserErrors.NotFound);
            }
            if (!_passwordHasher.Verify(command.CurrentPassword, user.PasswordHash))
            {
                return Result.Failure(UserErrors.InvalidPassword);
            }
            var newPasswordHash = _passwordHasher.Hash(command.NewPassword);
            user.UpdatePassword(command.NewPassword, newPasswordHash);
            _db.Users.Update(user);
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
    }

    /// <summary>
    /// Carter endpoint that maps <c>POST /change-password</c>.
    /// Requires authorization. Returns <c>200 OK</c> on success or Problem Details on failure.
    /// </summary>
    public sealed class Endpoint : IEndpoint
    {
        /// <summary>Registers the change-password route.</summary>
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/change-password", async (
                ChangePasswordCommand command,
                ICommandHandler<ChangePasswordCommand> handler,
                CancellationToken ct = default) =>
            {
                var result = await handler.Handle(command, ct);
                return result.IsSuccess ? Results.Ok() : result.Problem();
            }).RequireAuthorization()
            .WithTags($"{nameof(User)}s")
            .WithSummary("Change password")
            .WithDescription("Updates the authenticated user's password. Requires current password verification.");
        }
    }
}
