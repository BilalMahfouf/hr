using Identity.Abstracions;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Shared.Abstracions;
using Shared.CQRS;
using Shared.Endpoints;
using Shared.Results;

namespace Identity.Application.Users;

public static class Register
{
    public record RegisterCommand(
        string Email,
        string Password,
        string UserName,
        string FirstName,
        string LastName) : ICommand<Login.Response>;

    public class RegisterCommandHandler
        : ICommandHandler<RegisterCommand, Login.Response>
    {
        private readonly IIdentityApplicationDbContext _db;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtProvider _jwtProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RegisterCommandHandler(
            IIdentityApplicationDbContext db,
            IPasswordHasher passwordHasher,
            IJwtProvider jwtProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _jwtProvider = jwtProvider;
            _httpContextAccessor = httpContextAccessor;
        }

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

    public class Endpoint : IEndpoint
    {
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
