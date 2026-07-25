using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Common.CQRS;
using VeterinaryApi.Common.Endpoints;
using VeterinaryApi.Common.Results;
using VeterinaryApi.Domain.Users;

namespace VeterinaryApi.Features.Users;

/// <summary>
/// Vertical slice for registering a new user account.
/// Hashes the password, validates email uniqueness, creates the <see cref="User"/> aggregate,
/// and issues a JWT access token + 7-day HTTP-only refresh token cookie — identical issuance
/// flow to <see cref="Login"/>.
/// </summary>
public static class Register
{
    /// <summary>
    /// Command carrying all fields required to create a new user account.
    /// Implements <see cref="ICommand{TResponse}"/> reusing <see cref="Login.Response"/> as the result.
    /// </summary>
    /// <param name="Email">Must be unique across all users.</param>
    /// <param name="Password">Plain-text password to be hashed with Argon2 before persistence.</param>
    /// <param name="UserName">Display username for the account.</param>
    /// <param name="FirstName">User's first name.</param>
    /// <param name="LastName">User's last name.</param>
    public record RegisterCommand(
        string Email,
        string Password,
        string UserName,
        string FirstName,
        string LastName) : ICommand<Login.Response>;

    /// <summary>
    /// Handles the <see cref="RegisterCommand"/> by hashing the password, creating the user entity,
    /// generating authentication tokens, persisting the session, and setting the refresh token cookie.
    /// </summary>
    public class RegisterCommandHandler
        : ICommandHandler<RegisterCommand, Login.Response>
    {
        private readonly IApplicationDbContext _db;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtProvider _jwtProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>Initializes the handler with all required authentication services.</summary>
        public RegisterCommandHandler(
            IApplicationDbContext db,
            IPasswordHasher passwordHasher,
            IJwtProvider jwtProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _jwtProvider = jwtProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Executes the registration flow:
        /// <list type="number">
        ///   <item>Hashes the plain-text password with Argon2.</item>
        ///   <item>Checks that the email is not already registered.</item>
        ///   <item>Creates a <see cref="User"/> aggregate via <c>User.Register()</c> (defaults role to <c>Doctor</c>).</item>
        ///   <item>Generates a JWT access token and an opaque refresh token.</item>
        ///   <item>Persists both the new user and a <see cref="UserSession"/> record (7-day expiry) in one <c>SaveChangesAsync</c> call.</item>
        ///   <item>Writes the refresh token to an HTTP-only, Secure, SameSite=None cookie.</item>
        /// </list>
        /// </summary>
        /// <param name="command">The registration command with user details.</param>
        /// <param name="cancellationToken">Token for cooperative cancellation.</param>
        /// <returns>
        /// A successful result containing the JWT access token, or a failure with
        /// <c>UserErrors.EmailAlreadyInUse</c> when the email is taken.
        /// </returns>
        public async Task<Result<Login.Response>> Handle(
            RegisterCommand command,
            CancellationToken cancellationToken = default)
        {
            var hashPassword = _passwordHasher.Hash(command.Password);

            var isEmailInUse = await _db.Users
                .AnyAsync(u => u.Email == command.Email, cancellationToken);
            if (isEmailInUse)
            {
                return Result<Login.Response>
                    .Failure(UserErrors.EmailAlreadyInUse(command.Email));
            }

            var user = User.Register(
                command.UserName,
                command.FirstName,
                command.LastName,
                command.Email,
                hashPassword);
            _db.Users.Add(user);

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
            var response = new Login.Response(token);

            return Result<Login.Response>.Success(response);
        }
    }

    /// <summary>
    /// Carter endpoint that maps <c>POST /auth/register</c>.
    /// Returns <c>200 OK</c> with the access token, or a Problem Details error on failure.
    /// </summary>
    public class Endpoint : IEndpoint
    {
        /// <summary>Registers the registration route.</summary>
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/auth/register", async (
                RegisterCommand command,
                ICommandHandler<RegisterCommand, Login.Response> hander,
                CancellationToken cancellationToken = default) =>
            {
                var result = await hander.Handle(command, cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Problem();
            })
            .WithTags("Authentication")
            .WithSummary("Register a new user")
            .WithDescription("Creates a new user account with email, password, username, first name, and last name.");
        }
    }
}
