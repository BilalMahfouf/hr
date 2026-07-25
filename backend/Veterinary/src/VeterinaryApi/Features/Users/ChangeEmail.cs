using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Common.CQRS;
using VeterinaryApi.Common.Endpoints;
using VeterinaryApi.Common.Results;
using VeterinaryApi.Domain.Users;

namespace VeterinaryApi.Features.Users;

/// <summary>
/// Vertical slice for changing the authenticated user's email address.
/// Validates the new email format, checks global uniqueness, and delegates the update
/// to the <see cref="Domain.Users.User"/> aggregate.
/// </summary>
public static class ChangeEmail
{
    /// <summary>
    /// Command carrying the new email address.
    /// Implements the non-generic <see cref="ICommand"/> (no return value).
    /// </summary>
    /// <param name="Email">The desired new email address. Must be unique across all users.</param>
    public sealed record ChangeEmailCommand(string Email) : ICommand;

    /// <summary>
    /// FluentValidation validator for <see cref="ChangeEmailCommand"/>.
    /// Enforces that the email is non-empty and a valid email format.
    /// </summary>
    public sealed class Validator : AbstractValidator<ChangeEmailCommand>
    {
        /// <summary>Configures the email validation rules.</summary>
        public Validator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();
        }
    }

    /// <summary>
    /// Handles the <see cref="ChangeEmailCommand"/> by validating, checking uniqueness,
    /// and updating the user's email address.
    /// </summary>
    public sealed class ChangeEmailCommandHandler
        : ICommandHandler<ChangeEmailCommand>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentTenant _currentTenant;
        private readonly IValidator<ChangeEmailCommand> _validator;

        /// <summary>Initializes the handler with database, tenant context, and validator.</summary>
        public ChangeEmailCommandHandler(
            IApplicationDbContext db,
            ICurrentTenant currentTenant,
            IValidator<ChangeEmailCommand> validator)
        {
            _db = db;
            _currentTenant = currentTenant;
            _validator = validator;
        }

        /// <summary>
        /// Executes the change-email flow:
        /// <list type="number">
        ///   <item>Runs FluentValidation via <c>ValidateAndThrow</c> (throws <c>ValidationException</c> on failure).</item>
        ///   <item>Checks that the new email is not already in use by another user.</item>
        ///   <item>Loads the current user via <see cref="ICurrentTenant"/>.</item>
        ///   <item>Calls <c>User.UpdateEmail()</c> and persists the change.</item>
        /// </list>
        /// </summary>
        /// <param name="command">The change-email command with the new address.</param>
        /// <param name="cancellationToken">Token for cooperative cancellation.</param>
        /// <returns>
        /// A successful result, or a failure with <c>UserErrors.EmailAlreadyInUse</c> or
        /// <c>UserErrors.UserNotFound</c>.
        /// </returns>
        public async Task<Result> Handle(
            ChangeEmailCommand command,
            CancellationToken cancellationToken)
        {
            _validator.ValidateAndThrow(command);

            var isEmailInUse = await _db.Users
                .AnyAsync(u => u.Email == command.Email);
            if (isEmailInUse)
            {
                return Result.Failure(UserErrors.EmailAlreadyInUse(command.Email));
            }

            var user = await _db.Users
                .FirstOrDefaultAsync(
                e => e.Id == _currentTenant.UserId,
                cancellationToken);
            if (user is null)
            {
                return Result.Failure(UserErrors.UserNotFound(command.Email));
            }
            user.UpdateEmail(command.Email);
            _db.Users.Update(user);
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
    }

    /// <summary>
    /// Carter endpoint that maps <c>PATCH /change-email</c>.
    /// Requires authorization. Returns <c>204 No Content</c> on success or Problem Details on failure.
    /// </summary>
    public sealed class Endpoint : IEndpoint
    {
        /// <summary>Registers the change-email route.</summary>
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPatch("/change-email", async (
                ChangeEmailCommand command,
                ICommandHandler<ChangeEmailCommand> handler,
                CancellationToken ct = default) =>
            {
                var result = await handler.Handle(command, ct);
                return result.IsSuccess ? Results.NoContent() : result.Problem();
            }).RequireAuthorization()
            .WithTags($"{nameof(User)}s")
            .WithSummary("Change email")
            .WithDescription("Updates the authenticated user's email address. Requires the new email to be unique.");
        }
    }
}
