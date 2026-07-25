using Carter;
using Carter.OpenApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Windows.Input;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Common.CQRS;
using VeterinaryApi.Common.Endpoints;
using VeterinaryApi.Common.Results;
using VeterinaryApi.Domain.Users;

namespace VeterinaryApi.Features.Users;

/// <summary>
/// Vertical slice for rotating authentication tokens.
/// Validates the existing refresh token, issues a new JWT access token,
/// rotates the refresh token (prevents replay attacks), and updates the cookie.
/// </summary>
public static class RefreshToken
{
    /// <summary>
    /// Command carrying the current refresh token to exchange.
    /// Implements <see cref="ICommand{TResponse}"/> where the response is <see cref="Response"/>.
    /// </summary>
    /// <param name="RefreshToken">The opaque refresh token read from the HTTP-only cookie.</param>
    public record RefreshTokenCommand(string RefreshToken)
        : ICommand<Response>;

    /// <summary>Response DTO containing the newly issued JWT access token.</summary>
    /// <param name="Token">The new signed JWT access token.</param>
    public record Response(string Token);

    /// <summary>
    /// Handles the <see cref="RefreshTokenCommand"/> by validating the session,
    /// generating rotated tokens, updating the persisted session, and refreshing the cookie.
    /// </summary>
    public sealed class RefreshTokenCommandHandler
        : ICommandHandler<RefreshTokenCommand, Response>
    {
        private readonly IApplicationDbContext _db;
        private readonly IJwtProvider _jwtProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>Initializes the handler with required services.</summary>
        public RefreshTokenCommandHandler(
            IApplicationDbContext db,
            IJwtProvider jwtProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _jwtProvider = jwtProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Executes the token refresh flow:
        /// <list type="number">
        ///   <item>Loads the <see cref="UserSession"/> including the associated <see cref="User"/>.</item>
        ///   <item>Returns <c>UserErrors.InvalidCredentials</c> if the session does not exist.</item>
        ///   <item>Returns <c>UserErrors.ExpiredRefreshToken</c> if the session has expired.</item>
        ///   <item>Generates a new JWT access token and a new opaque refresh token.</item>
        ///   <item>Rotates the session token and persists the update.</item>
        ///   <item>Writes the new refresh token to the HTTP-only, Secure, SameSite=None cookie (7-day expiry).</item>
        /// </list>
        /// </summary>
        /// <param name="command">The command containing the current refresh token.</param>
        /// <param name="cancellationToken">Token for cooperative cancellation.</param>
        /// <returns>A successful result with the new JWT, or a failure result.</returns>
        public async Task<Result<Response>> Handle(
            RefreshTokenCommand command,
            CancellationToken cancellationToken = default)
        {
            var session = await _db.UserSessions
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Token == command.RefreshToken);
            if (session is null)
            {
                return Result<Response>.Failure(UserErrors.InvalidCredentials);
            }
            if (session.ExpiresAt < DateTime.Now)
            {
                return Result<Response>.Failure(UserErrors.ExpiredRefreshToken);
            }
            var token = _jwtProvider.GenerateToken(session.User);
            var refreshToken = _jwtProvider.GenerateRefreshToken();
            session.Token = refreshToken;

            _db.UserSessions.Update(session);
            await _db.SaveChangesAsync();

            _httpContextAccessor.HttpContext!.Response
                .Cookies.Append("refreshToken", refreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Expires = DateTimeOffset.UtcNow.AddDays(7),
                    SameSite = SameSiteMode.None,
                    Secure = true
                });
            var response = new Response(token);
            return Result<Response>.Success(response);
        }
    }

    /// <summary>
    /// Carter endpoint that maps <c>POST /auth/refresh-token</c>.
    /// Reads the refresh token from the incoming cookie, exchanges it for a new access token,
    /// and rotates the cookie. Returns <c>200 OK</c> with the new token, or Problem Details.
    /// </summary>
    public class Endpoint : ICarterModule
    {
        /// <summary>Registers the refresh token route.</summary>
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/auth/refresh-token", async (
                HttpContext httpContext,
                [FromServices] ICommandHandler<RefreshTokenCommand, Response> handler,
                CancellationToken cancellationToken = default) =>
            {
                var refreshToken = httpContext.Request
                .Cookies["refreshToken"] ?? string.Empty;

                var command = new RefreshTokenCommand(refreshToken);
                var result = await handler.Handle(command, cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Problem();
            })
            .WithTags("Authentication")
            .WithSummary("Refresh access token")
            .WithDescription("Generates a new JWT access token using the refresh token from cookies. Also rotates the refresh token for security.");
        }
    }
}

