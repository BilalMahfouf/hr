
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Common.CQRS;
using VeterinaryApi.Common.Endpoints;
using VeterinaryApi.Common.Results;
using VeterinaryApi.Domain.Users;

namespace VeterinaryApi.Features.Users;

/// <summary>
/// Vertical slice for logging out the current user.
/// Reads the refresh token from the cookie, removes the <see cref="UserSession"/> record,
/// and deletes the cookie from the response.
/// </summary>
public class Logout
{
    /// <summary>
    /// Command carrying the refresh token to invalidate.
    /// Implements the non-generic <see cref="ICommand"/> (no return value).
    /// </summary>
    /// <param name="RefreshToken">The opaque refresh token read from the HTTP-only cookie.</param>
    public sealed record LogoutCommand(string RefreshToken)
        : ICommand;

    /// <summary>
    /// Handles the <see cref="LogoutCommand"/> by finding and deleting the matching
    /// <see cref="UserSession"/>, then removing the refresh token cookie.
    /// </summary>
    public sealed class LogoutCommandHandler
        : ICommandHandler<LogoutCommand>
    {
        private readonly IApplicationDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>Initializes the handler with required services.</summary>
        public LogoutCommandHandler(
            IApplicationDbContext db,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Executes the logout flow:
        /// <list type="number">
        ///   <item>Looks up the <see cref="UserSession"/> by refresh token.</item>
        ///   <item>Removes the session row from the database.</item>
        ///   <item>Deletes the <c>refreshToken</c> cookie from the HTTP response.</item>
        /// </list>
        /// </summary>
        /// <param name="command">The logout command containing the refresh token.</param>
        /// <param name="cancellationToken">Token for cooperative cancellation.</param>
        /// <returns>A successful result, or <c>UserErrors.InvalidCredentials</c> if the token is not found.</returns>
        public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken = default)
        {
            var session = await _db.UserSessions
                .FirstOrDefaultAsync(e => e.Token == command.RefreshToken);
            if (session is null)
            {
                return Result.Failure(UserErrors.InvalidCredentials);
            }
            _db.UserSessions.Remove(session);
            await _db.SaveChangesAsync(cancellationToken);

            _httpContextAccessor.HttpContext!.Response.Cookies.Delete("refreshToken");
            return Result.Success;
        }
    }

    /// <summary>
    /// Carter endpoint that maps <c>POST /auth/logout</c>.
    /// Reads the refresh token from the incoming cookie and invokes the logout handler.
    /// Returns <c>200 OK</c> on success or a Problem Details error.
    /// </summary>
    public class Endpoint : IEndpoint
    {
        /// <summary>Registers the logout route.</summary>
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/auth/logout", async (
               HttpContext httpContext,
               [FromServices] ICommandHandler<LogoutCommand> handler,
                CancellationToken cancellationToken = default) =>
            {
                var refreshToken = httpContext.Request
                .Cookies["refreshToken"] ?? string.Empty;
                var command = new LogoutCommand(refreshToken);
                var result = await handler.Handle(command, cancellationToken);
                return result.IsSuccess ? Results.Ok() : result.Problem();
            })
            .WithTags("Authentication")
            .WithSummary("User logout")
            .WithDescription("Logs out the current user by invalidating the refresh token and clearing the refresh token cookie.");
        }
    }
}
