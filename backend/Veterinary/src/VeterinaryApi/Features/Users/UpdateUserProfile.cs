using Microsoft.EntityFrameworkCore;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Common.CQRS;
using VeterinaryApi.Common.Endpoints;
using VeterinaryApi.Common.Results;
using VeterinaryApi.Domain.Users;

namespace VeterinaryApi.Features.Users;

/// <summary>
/// Vertical slice for updating the authenticated user's profile information.
/// Only the display fields (username, first name, last name) are updatable here;
/// email and password changes have dedicated endpoints.
/// </summary>
public static class UpdateUserProfile
{
    /// <summary>
    /// Command carrying the new profile values.
    /// Implements the non-generic <see cref="ICommand"/> (no return value).
    /// </summary>
    /// <param name="UserName">New display username.</param>
    /// <param name="FirstName">New first name.</param>
    /// <param name="LastName">New last name.</param>
    public sealed record UpdateUserProfileCommand(
        string UserName,
        string FirstName,
        string LastName) : ICommand;

    /// <summary>
    /// Handles the <see cref="UpdateUserProfileCommand"/> by loading the current user
    /// and delegating the update to <c>User.UpdateProfile()</c>.
    /// </summary>
    public sealed class UpdateUserProfileCommandHandler
        : ICommandHandler<UpdateUserProfileCommand>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentTenant _currentTenant;

        /// <summary>Initializes the handler with database and tenant context services.</summary>
        public UpdateUserProfileCommandHandler(
            IApplicationDbContext db,
            ICurrentTenant currentTenant)
        {
            _db = db;
            _currentTenant = currentTenant;
        }

        /// <summary>
        /// Executes the profile update flow:
        /// <list type="number">
        ///   <item>Resolves the current user ID from <see cref="ICurrentTenant"/>.</item>
        ///   <item>Loads the user entity.</item>
        ///   <item>Calls <c>User.UpdateProfile()</c> with the new values.</item>
        ///   <item>Persists the change.</item>
        /// </list>
        /// </summary>
        /// <param name="command">The update-profile command.</param>
        /// <param name="cancellationToken">Token for cooperative cancellation.</param>
        /// <returns>A successful result, or a failure with <c>UserErrors.NotFound</c>.</returns>
        public async Task<Result> Handle(
            UpdateUserProfileCommand command,
            CancellationToken cancellationToken = default)
        {
            var userId = _currentTenant.UserId;
            var user = await _db.Users
                .FirstOrDefaultAsync(e => e.Id == userId, cancellationToken);
            if (user is null)
            {
                return Result.Failure(UserErrors.NotFound);
            }
            user.UpdateProfile(command.UserName, command.FirstName, command.LastName);
            _db.Users.Update(user);
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
    }

    /// <summary>
    /// Carter endpoint that maps <c>PUT /update-profile</c>.
    /// Requires authorization. Returns <c>204 No Content</c> on success or Problem Details on failure.
    /// </summary>
    public sealed class Endpoint : IEndpoint
    {
        /// <summary>Registers the update-profile route.</summary>
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPut("/update-profile", async (
                UpdateUserProfileCommand command,
                ICommandHandler<UpdateUserProfileCommand> handler) =>
            {
                var result = await handler.Handle(command);
                return result.IsSuccess ? Results.NoContent() : result.Problem();
            }).RequireAuthorization()
            .WithTags($"{nameof(User)}s")
            .WithSummary("Update user profile")
            .WithDescription("Updates the authenticated user's profile information including username and name.");
        }
    }
}
