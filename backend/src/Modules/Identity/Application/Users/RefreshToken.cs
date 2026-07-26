using Identity.Abstracions;
using Identity.Domain.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Common.CQRS;
using VeterinaryApi.Common.Endpoints;
using VeterinaryApi.Common.Results;

namespace Identity.Application.Users;

public static class RefreshToken
{
    public record RefreshTokenCommand(string RefreshToken)
        : ICommand<Response>;

    public record Response(string Token);

    public sealed class RefreshTokenCommandHandler
        : ICommandHandler<RefreshTokenCommand, Response>
    {
        private readonly IIdentityApplicationDbContext _db;
        private readonly IJwtProvider _jwtProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RefreshTokenCommandHandler(
            IIdentityApplicationDbContext db,
            IJwtProvider jwtProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _jwtProvider = jwtProvider;
            _httpContextAccessor = httpContextAccessor;
        }

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

    public class Endpoint : IEndpoint
    {
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
