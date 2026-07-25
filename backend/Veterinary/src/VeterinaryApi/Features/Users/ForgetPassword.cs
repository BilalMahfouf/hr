using Microsoft.EntityFrameworkCore;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Common.Abstracions.Emails;
using VeterinaryApi.Common.CQRS;
using VeterinaryApi.Common.Endpoints;
using VeterinaryApi.Common.Results;
using VeterinaryApi.Common.Util;
using VeterinaryApi.Domain.Users;

namespace VeterinaryApi.Features.Users;

/// <summary>
/// Vertical slice for initiating a password reset flow.
/// Looks up the user by email, creates a short-lived <see cref="UserSession"/> with type
/// <see cref="UserSessionTokenType.ResetPassword"/> (15-minute expiry), and emails the user
/// a reset link containing the token.
/// </summary>
public static class ForgetPassword
{
    /// <summary>
    /// Command to request a password reset email.
    /// Implements the non-generic <see cref="ICommand"/> (no return value).
    /// </summary>
    /// <param name="Email">The email address of the account to reset.</param>
    /// <param name="ClientUri">The front-end URL base used to construct the reset link embedded in the email.</param>
    public record ForgetPasswordCommand(string Email, string ClientUri)
        : ICommand;

    /// <summary>Empty response record (unused; kept for potential future extension).</summary>
    public record Response();

    /// <summary>
    /// Handles the <see cref="ForgetPasswordCommand"/> by generating a reset token,
    /// persisting a short-lived session, and sending a password reset email.
    /// </summary>
    public class ForgetPasswordCommandHandler
        : ICommandHandler<ForgetPasswordCommand>
    {
        private readonly IApplicationDbContext _db;
        private readonly IJwtProvider _jwtProvider;
        private readonly IEmailService _emailService;

        /// <summary>Initializes the handler with database, JWT, and email services.</summary>
        public ForgetPasswordCommandHandler(
            IApplicationDbContext db,
            IJwtProvider jwtProvider,
            IEmailService emailService)
        {
            _db = db;
            _jwtProvider = jwtProvider;
            _emailService = emailService;
        }

        /// <summary>
        /// Executes the forget-password flow:
        /// <list type="number">
        ///   <item>Looks up the user by <paramref name="command"/>.Email.</item>
        ///   <item>Returns <c>UserErrors.UserNotFound</c> if the email is not registered.</item>
        ///   <item>Generates a JWT as the reset token (shares the JWT infrastructure for convenience).</item>
        ///   <item>Creates a <see cref="UserSession"/> with <see cref="UserSessionTokenType.ResetPassword"/> and 15-minute expiry.</item>
        ///   <item>Builds a reset link via <c>Utility.GenerateResponseLink</c> and sends an HTML email.</item>
        /// </list>
        /// </summary>
        /// <param name="command">The forget-password command with email and client URI.</param>
        /// <param name="cancellationToken">Token for cooperative cancellation.</param>
        /// <returns>A successful result, or a failure if the email is not found.</returns>
        public async Task<Result> Handle(
            ForgetPasswordCommand command,
            CancellationToken cancellationToken = default)
        {
            var user = await _db.Users
               .FirstOrDefaultAsync(e => e.Email == command.Email,
               cancellationToken);
            if (user is null)
            {
                return Result<Response>
                    .Failure(UserErrors.UserNotFound(command.Email));
            }
            var token = _jwtProvider.GenerateToken(user);
            var userSession = new UserSession()
            {
                UserId = user.Id,
                Token = token,
                TokenType = UserSessionTokenType.ResetPassword,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };
            user.ForgetPassword(token, command.ClientUri);
            _db.UserSessions.Add(userSession);
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
    }
    /// <summary>
    /// Carter endpoint that maps <c>POST /auth/forget-password</c>.
    /// Returns <c>200 OK</c> on success or Problem Details on failure.
    /// </summary>
    public class Endpoint : IEndpoint
    {
        /// <summary>Registers the forget-password route.</summary>
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/auth/forget-password", async (
                ForgetPasswordCommand command,
                ICommandHandler<ForgetPasswordCommand> handler,
                CancellationToken cancellationToken = default) =>
            {
                var result = await handler.Handle(command, cancellationToken);
                return result.IsSuccess ? Results.NoContent() : result.Problem();
            })
            .WithTags("Authentication")
            .WithSummary("Request password reset")
            .WithDescription("Sends a password reset email to the specified email address. The email contains a link with a reset token valid for 15 minutes.");
        }
    }
}
