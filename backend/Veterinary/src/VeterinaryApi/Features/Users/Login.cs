using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Common.CQRS;
using VeterinaryApi.Common.Endpoints;
using VeterinaryApi.Common.Results;
using VeterinaryApi.Domain.Users;

namespace VeterinaryApi.Features.Users;

/// <summary>
/// Vertical slice for authenticating an existing user with email and password.
/// On success, issues a short-lived JWT access token and a 7-day HTTP-only refresh token cookie.
/// </summary>
public static class Login
{
    /// <summary>Response DTO returned on successful authentication.</summary>
    /// <param name="Token">The signed JWT access token to be used as a Bearer token in subsequent requests.</param>
    public record Response(string Token);

    /// <summary>
    /// Command carrying the user's credentials.
    /// Implements <see cref="ICommand{TResponse}"/> where the response is <see cref="Response"/>.
    /// </summary>
    /// <param name="Email">The user's registered email address.</param>
    /// <param name="Password">The plain-text password to verify against the stored hash.</param>
    public record LoginCommand(string Email, string Password)
        : ICommand<Response>;

    /// <summary>
    /// Handles the <see cref="LoginCommand"/> by looking up the user by email,
    /// verifying the password hash, generating tokens, persisting the session, and
    /// writing the refresh token to an HTTP-only secure cookie.
    /// </summary>
    public class LoginCommandHandler : ICommandHandler<LoginCommand, Response>
    {
        private readonly IApplicationDbContext _db;
        private readonly IPasswordHasher _passwordHahser;
        private readonly IJwtProvider _jwtProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>Initializes the handler with all required authentication services.</summary>
        public LoginCommandHandler(
            IApplicationDbContext db,
            IPasswordHasher passwordHahser,
            IJwtProvider jwtProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _passwordHahser = passwordHahser;
            _jwtProvider = jwtProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Executes the login flow:
        /// <list type="number">
        ///   <item>Looks up the user by email.</item>
        ///   <item>Verifies the provided password against the stored Argon2 hash.</item>
        ///   <item>Generates a new JWT access token and an opaque refresh token.</item>
        ///   <item>Persists a <see cref="UserSession"/> record for the refresh token (7-day expiry).</item>
        ///   <item>Writes the refresh token to an HTTP-only, Secure, SameSite=None cookie.</item>
        /// </list>
        /// </summary>
        /// <param name="command">The login command with email and password.</param>
        /// <param name="cancellationToken">Token for cooperative cancellation.</param>
        /// <returns>
        /// A successful result containing the JWT access token, or a failure with
        /// <c>UserErrors.UserNotFound</c> or <c>UserErrors.InvalidCredentials</c>.
        /// </returns>
        public async Task<Result<Response>> Handle(
            LoginCommand command,
            CancellationToken cancellationToken = default)
        {
            var user = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(
                e => e.Email == command.Email,
                cancellationToken);

            if (user is null)
            {
                var error = UserErrors.UserNotFound(command.Email);
                return Result<Response>.Failure(error);
            }

            bool validPassword = _passwordHahser.Verify(command.Password, user.PasswordHash);
            if (!validPassword)
            {
                return Result<Response>.Failure(UserErrors.InvalidCredentials);
            }

            var token = _jwtProvider.GenerateToken(user);
            var refreshToken = _jwtProvider.GenerateRefreshToken();

            var userSession = new UserSession
            {
                UserId = user.Id,
                Token = refreshToken,
                TokenType = UserSessionTokenType.Refresh,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
            };

            _db.UserSessions.Add(userSession);
            await _db.SaveChangesAsync(cancellationToken);

            // Write refresh token as a secure, HTTP-only cookie to prevent XSS exposure.
            _httpContextAccessor.HttpContext!.Response
                .Cookies.Append(
                "refreshToken",
                refreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Expires = userSession.ExpiresAt,
                    SameSite = SameSiteMode.None,
                    Secure = true,
                });

            var response = new Response(token);
            return Result<Response>.Success(response);
        }
    }

    /// <summary>
    /// Carter endpoint that maps <c>POST /auth/login</c>.
    /// Returns <c>200 OK</c> with the access token, or a Problem Details error on failure.
    /// </summary>
    public class Endpoint : IEndpoint
    {
        /// <summary>Registers the login route.</summary>
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("auth/login", async (
                [FromBody] Login.LoginCommand request,
                [FromServices] ICommandHandler<LoginCommand, Response> handler,
                CancellationToken cancellationToken = default) =>
            {
                var result = await handler.Handle(request, cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Problem();
            })
            .WithTags("Authentication")
            .WithSummary("User login")
            .WithDescription("Authenticates a user with email and password. Returns a JWT access token and sets a refresh token cookie.");
        }
    }
}
